using System.Collections.Generic;
using Fusion;
using UnityEngine;
using VoidRogues.NonPlayerCharacters;

namespace VoidRogues
{
    /// <summary>
    /// Visual-only client-side component that pushes nearby NPC transforms away from the
    /// local player, compensating for the network round-trip delay before the
    /// server-authoritative separation arrives.
    ///
    /// <para>
    /// Iterates active NPCs from <see cref="NonPlayerCharacterManager"/> each frame to find
    /// those close enough to seed, reading visual transform positions directly rather than
    /// relying on physics queries (which lag behind because NPC transforms are set in Fusion's
    /// <c>Render()</c> / Unity <c>Update</c> but the physics broadphase only syncs at
    /// <c>FixedUpdate</c> when <c>Physics.autoSyncTransforms</c> is false).
    /// Once an NPC enters the exclusion circle it is tracked until its display position
    /// converges back to the network position after the server push has landed.
    /// </para>
    ///
    /// <para>
    /// The push math is identical to the former <c>ApplyPredictiveClientSeparation</c>
    /// pass in <see cref="NonPlayerCharacterManager"/>:
    /// proportional repulsion force → RVO-style lateral flocking → damped-spring
    /// reconciliation once the server position exits the circle.
    /// </para>
    ///
    /// <para>
    /// Runs in <see cref="LateUpdate"/> so it executes after Fusion's <c>Render()</c>
    /// has placed all NPC transforms at their interpolated network positions.
    /// Only active when the component has input authority but not state authority
    /// (i.e. the local player on a client, not the server).
    /// </para>
    /// </summary>
    public class NpcFlockingComponent : ContextBehaviour
    {
        [Header("Separation Radius")]
        [SerializeField]
        [Tooltip("Logical radius of the player used for NPC overlap tests (world units). " +
                 "Should match the KCC capsule radius on the PlayerCharacter prefab.")]
        private float _playerSeparationRadius = 0.35f;

        [SerializeField]
        [Tooltip("Logical radius of each NPC used for overlap tests (world units).")]
        private float _npcSeparationRadius = 0.4f;

        [SerializeField]
        [Tooltip("Extra gap added on top of the combined radius to prevent tight sliding contact.")]
        private float _separationSkinWidth = 0.02f;

        [Header("Push Forces")]
        [SerializeField]
        [Tooltip("Repulsion force applied per unit of penetration depth when an NPC's display " +
                 "position is inside the exclusion circle (units/s² per unit of overlap). " +
                 "Higher values push NPCs out faster; lower values give a gentler, slower push.")]
        private float _separationPushForce = 40f;

        [SerializeField]
        [Tooltip("Scales the lateral flocking force relative to the main push force. " +
                 "0.1 = flocking nudge is 10% of the push; increase for more aggressive sideways spreading.")]
        private float _npcFlockingForceScale = 0.1f;

        [SerializeField]
        [Tooltip("Radius within which two pushed NPCs repel each other, " +
                 "causing them to spread sideways instead of piling up (world units).")]
        private float _npcFlockingRadius = 1.2f;

        [SerializeField]
        [Tooltip("Spring stiffness used when reconciling a pushed NPC's display position back to the " +
                 "network position once the server push has landed.")]
        private float _reconcileSpringStrength = 25f;

        [SerializeField]
        [Tooltip("Velocity damping coefficient applied per second during spring reconciliation. " +
                 "For critical damping use damping ≈ 2 * sqrt(strength).")]
        private float _reconcileSpringDamping = 10f;

        // Minimum squared magnitude used when checking whether a computed push vector is
        // effectively zero (avoids normalising near-zero vectors).
        private const float EPSILON_SQUARED = 1e-8f;

        // Minimum XZ distance at which we consider two centres non-coincident and can
        // derive a reliable push direction from their delta.
        private const float DISTANCE_EPSILON = 1e-4f;

        // Squared convergence threshold: when a decaying display position is within this
        // distance² of the network position and velocity is near zero, the entry is removed.
        private const float CONVERGENCE_THRESHOLD_SQUARED          = 0.01f;
        private const float CONVERGENCE_VELOCITY_THRESHOLD_SQUARED = 0.04f;

        // Tracks the visual (display) XZ position of each NPC that is currently being
        // pushed. Entries exist while an NPC is inside (or recently exited) the exclusion
        // circle. Keyed by NonPlayerCharacter instance (reference identity).
        private readonly Dictionary<NonPlayerCharacter, Vector3> _displayPositions =
            new Dictionary<NonPlayerCharacter, Vector3>();

        // XZ display velocity (world units/second) per NPC, maintained across frames so
        // the spring reconciliation can blend outward push momentum into the return arc.
        private readonly Dictionary<NonPlayerCharacter, Vector2> _displayVelocities =
            new Dictionary<NonPlayerCharacter, Vector2>();

        // Reusable list for collecting keys to remove, to avoid mutating the dict during iteration.
        private readonly List<NonPlayerCharacter> _toRemove = new List<NonPlayerCharacter>();

        // Snapshot of _displayPositions taken at the start of each separation pass.
        // Iterating over this snapshot lets us safely write back to _displayPositions and also
        // gives the flocking inner loop consistent start-of-frame positions (correct RVO behaviour).
        private readonly Dictionary<NonPlayerCharacter, Vector3> _displayPositionsSnapshot =
            new Dictionary<NonPlayerCharacter, Vector3>();

        // ── NetworkBehaviour lifecycle ────────────────────────────────────────────────

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _displayPositions.Clear();
            _displayVelocities.Clear();
        }

        // ── Unity lifecycle ───────────────────────────────────────────────────────────

        private void LateUpdate()
        {
            // Run only on the machine that controls this PlayerCharacter (HasInputAuthority).
            // We deliberately do NOT check HasStateAuthority here: in Shared mode (GameMode.Shared)
            // each player has state authority over their own PlayerCharacter, so requiring
            // !HasStateAuthority would silently disable predictive separation for every player.
            // The separation is still useful in Shared mode because NPC positions are owned by the
            // master client, not the local player, so there is still network latency to compensate.
            if (Runner == null || !HasInputAuthority)
                return;

            ApplyPredictiveClientSeparation();
        }

        private void ApplyPredictiveClientSeparation()
        {
            float combined   = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth;
            float combinedSq = combined * combined;
            float dt         = Time.deltaTime;

            Vector3 playerPos = transform.position;
            float dampFactor  = Mathf.Exp(-_reconcileSpringDamping * dt);
            float flockRadSq  = _npcFlockingRadius * _npcFlockingRadius;

            // ── 1. Seed newly-entered NPCs via the NPC manager ───────────────────────
            // NPC transforms are updated in Fusion's Render() (Unity Update phase).
            // Physics.autoSyncTransforms is false by default in Unity 2020+, so a
            // physics overlap query in LateUpdate would see stale positions from the
            // previous FixedUpdate.  Querying the NPC manager directly reads the
            // transform positions that were just placed by NonPlayerCharacterManager.Render().
            var npcManager = Context?.NonPlayerCharacterManager;
            if (npcManager == null)
                return;

            npcManager.ForEachActiveNPC(npc =>
            {
                if (_displayPositions.ContainsKey(npc))
                    return;

                Vector3 npcPos = npc.CachedTransform.position;
                float dx = npcPos.x - playerPos.x;
                float dz = npcPos.z - playerPos.z;
                if (dx * dx + dz * dz < combinedSq)
                {
                    // Seed at the current interpolated (network) position so the force
                    // integration pushes it out smoothly — no snap to the boundary.
                    _displayPositions[npc] = npcPos;
                    _displayVelocities[npc] = Vector2.zero;
                }
            });

            // ── 2. Process all tracked NPCs ───────────────────────────────────────────
            // Includes both NPCs currently inside the circle AND those decaying back to
            // their network positions after the server separation has landed.
            //
            // We snapshot _displayPositions before iterating so that:
            //   a) writes back to _displayPositions during the loop are safe (we are
            //      enumerating the snapshot, not the live dictionary), and
            //   b) the inner flocking pass reads consistent start-of-frame positions
            //      (correct RVO behaviour — each NPC sees where its neighbours *were*,
            //      not where they have already moved to this frame).
            _displayPositionsSnapshot.Clear();
            foreach (KeyValuePair<NonPlayerCharacter, Vector3> kvp in _displayPositions)
                _displayPositionsSnapshot[kvp.Key] = kvp.Value;

            _toRemove.Clear();
            foreach (KeyValuePair<NonPlayerCharacter, Vector3> pair in _displayPositionsSnapshot)
            {
                NonPlayerCharacter npc = pair.Key;

                // If the NPC has been recycled / deactivated, discard the tracking entry.
                if (npc == null || !npc.isActiveAndEnabled)
                {
                    _toRemove.Add(npc);
                    continue;
                }

                Vector3 displayPos = pair.Value;
                _displayVelocities.TryGetValue(npc, out Vector2 vel);

                Transform npcTransform = npc.CachedTransform;
                Vector3   networkPos   = npcTransform.position;   // interpolated from server snapshots

                float ndx     = networkPos.x - playerPos.x;
                float ndz     = networkPos.z - playerPos.z;
                bool  netInside = (ndx * ndx + ndz * ndz) < combinedSq;

                // ── 2a. Repulsion force (display position inside exclusion circle) ────
                // Proportional to penetration depth: the deeper the overlap the harder
                // the push, producing a smooth acceleration from rest with no abrupt jump.
                float ddx      = displayPos.x - playerPos.x;
                float ddz      = displayPos.z - playerPos.z;
                float dispDist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                float overlap  = combined - dispDist;
                if (overlap > 0f)
                {
                    float outDirX, outDirZ;
                    if (dispDist > DISTANCE_EPSILON)
                    {
                        float inv = 1f / dispDist;
                        outDirX   = ddx * inv;
                        outDirZ   = ddz * inv;
                    }
                    else
                    {
                        // Display position is exactly on the player.  Use the network→away
                        // direction as the push axis so overlapping NPCs naturally diverge
                        // rather than all snapping to the same arbitrary axis.
                        float ndMag = Mathf.Sqrt(ndx * ndx + ndz * ndz);
                        if (ndMag > DISTANCE_EPSILON)
                        {
                            float inv = 1f / ndMag;
                            outDirX   = ndx * inv;
                            outDirZ   = ndz * inv;
                        }
                        else
                        {
                            outDirX = 1f;
                            outDirZ = 0f;
                        }
                    }

                    float pushMag = overlap * _separationPushForce;
                    vel.x += outDirX * pushMag * dt;
                    vel.y += outDirZ * pushMag * dt;
                }

                // ── 2b. RVO-style flocking: spread NPCs sideways ──────────────────────
                // Gentle lateral repulsion between NPCs so they fan outward instead of
                // stacking radially.
                float avoidX = 0f, avoidZ = 0f;
                foreach (KeyValuePair<NonPlayerCharacter, Vector3> otherPair in _displayPositionsSnapshot)
                {
                    if (otherPair.Key == npc)
                        continue;

                    float ex     = displayPos.x - otherPair.Value.x;
                    float ez     = displayPos.z - otherPair.Value.z;
                    float distSq = ex * ex + ez * ez;
                    if (distSq < flockRadSq && distSq > EPSILON_SQUARED)
                    {
                        float dist   = Mathf.Sqrt(distSq);
                        float weight = (_npcFlockingRadius - dist) / _npcFlockingRadius;
                        avoidX += (ex / dist) * weight;
                        avoidZ += (ez / dist) * weight;
                    }
                }

                float flockScale = _separationPushForce * _npcFlockingForceScale;
                vel.x += avoidX * flockScale * dt;
                vel.y += avoidZ * flockScale * dt;

                // ── 2c. Spring reconciliation ─────────────────────────────────────────
                // Pull the display position back toward the network position once the
                // server authoritative push has landed.  Only applied when the network
                // position is already outside the circle so it never fights the outward
                // repulsion.
                if (!netInside)
                {
                    vel.x += (networkPos.x - displayPos.x) * _reconcileSpringStrength * dt;
                    vel.y += (networkPos.z - displayPos.z) * _reconcileSpringStrength * dt;
                }

                // ── 2d. Velocity damping ──────────────────────────────────────────────
                vel.x *= dampFactor;
                vel.y *= dampFactor;

                // ── 2e. Integrate position ────────────────────────────────────────────
                displayPos.x += vel.x * dt;
                displayPos.z += vel.y * dt;

                // ── 2f. Convergence check ─────────────────────────────────────────────
                // Once the server has resolved the separation, remove the tracking entry
                // when the display position is close enough to the network position and
                // velocity has settled.
                if (!netInside)
                {
                    float ex2 = displayPos.x - networkPos.x;
                    float ez2 = displayPos.z - networkPos.z;
                    if (ex2 * ex2 + ez2 * ez2 < CONVERGENCE_THRESHOLD_SQUARED &&
                        vel.x * vel.x + vel.y * vel.y < CONVERGENCE_VELOCITY_THRESHOLD_SQUARED)
                    {
                        _toRemove.Add(npc);
                        continue;
                    }
                }

                _displayPositions[npc] = displayPos;
                _displayVelocities[npc] = vel;
                npcTransform.position   = new Vector3(displayPos.x, networkPos.y, displayPos.z);
            }

            // ── 3. Remove converged / recycled entries ────────────────────────────────
            foreach (NonPlayerCharacter npc in _toRemove)
            {
                _displayPositions.Remove(npc);
                _displayVelocities.Remove(npc);
            }
        }
    }
}
