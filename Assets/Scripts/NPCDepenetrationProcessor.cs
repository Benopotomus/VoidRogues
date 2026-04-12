namespace VoidRogues
{
    using Fusion.Addons.KCC;
    using UnityEngine;
    using VoidRogues.NonPlayerCharacters;

    /// <summary>
    /// KCC processor that applies analytic capsule-vs-sphere depenetration against every
    /// active NPC by reading positions directly from the server-authoritative
    /// <see cref="NonPlayerCharacterManager"/> networked struct array.
    ///
    /// <b>Determinism and forward-prediction</b><br/>
    /// NPC positions in <c>FNonPlayerCharacterData.Position</c> are server-authoritative and
    /// replicated via Fusion's snapshot system.  During KCC resimulation on a client, Fusion
    /// restores <c>_npcDatas</c> to the correct historical snapshot before each resimulated tick,
    /// so this processor reads the same positions the server used — no correction pops.
    ///
    /// For the handful of forward-prediction ticks that extend beyond the latest received server
    /// snapshot, <c>FNonPlayerCharacterData.Velocity</c> (computed server-side each tick and
    /// replicated) is used to linearly extrapolate each NPC's position.  This keeps the predicted
    /// player position close to what the server will confirm, minimising corrections at 150 ms ping.
    ///
    /// <b>Physics layer design:</b>
    /// NPC prefabs must be placed on the "NPC" physics layer (layer 6), which is intentionally
    /// excluded from the KCC's <c>CollisionLayerMask</c> (layer 0 / Default only).
    /// This ensures the KCC never contacts NPC physics capsules directly, so this processor
    /// is the <em>sole</em> player-NPC separation mechanism on both server and client.
    ///
    /// Add as a prefab processor in the KCC component's Processors list on the PlayerCharacter prefab.
    /// The manager reference is resolved lazily on first use.
    /// </summary>
    public class NPCDepenetrationProcessor : KCCProcessor, IAfterMoveStep
    {
        // Run after EnvironmentProcessor (1000) and NoGravityXZMovementProcessor (1500)
        // so we apply a final position correction on top of fully-resolved movement.
        public const float Priority = -500f;
        public override float GetPriority(KCC kcc) => Priority;

        [SerializeField]
        [Tooltip("Collision radius for each NPC's logical shape (world units). " +
                 "Tune to match your NPC capsule/sphere visual size.")]
        private float _npcRadius = 0.4f;

        [SerializeField]
        [Tooltip("Extra separation gap added on top of the combined radius. " +
                 "Prevents tight sliding contact that can cause jitter.")]
        private float _skinWidth = 0.02f;

        [SerializeField]
        [Tooltip("Number of solver passes per KCC move step. " +
                 "More passes resolve simultaneous overlaps with several NPCs more accurately.")]
        [Range(1, 8)]
        private int _maxIterations = 3;

        private NonPlayerCharacterManager _npcManager;
        private bool _managerSearched;

        // IAfterMoveStep INTERFACE

        public void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            if (_npcManager == null)
            {
                if (_managerSearched)
                    return;

                _managerSearched = true;
                _npcManager = Object.FindFirstObjectByType<NonPlayerCharacterManager>();
                if (_npcManager == null)
                    return;
            }

            float kccRadius  = kcc.Settings.Radius;
            float kccHeight  = kcc.Settings.Height;
            float combined   = kccRadius + _npcRadius + _skinWidth;
            float combinedSq = combined * combined;

            // Iterative solver so clustered NPCs all resolve cleanly.
            for (int iter = 0; iter < _maxIterations; iter++)
            {
                bool anyPenetration = false;

                // Recalculate capsule endpoints at the start of each outer pass.
                Vector3 capsuleBottom = data.TargetPosition + new Vector3(0f, kccRadius, 0f);
                Vector3 capsuleTop    = data.TargetPosition + new Vector3(0f, kccHeight - kccRadius, 0f);

                // Iterate every slot in the fixed-size NPC array.
                // Slots with DefinitionID == 0 are free/recycled and are skipped cheaply.
                for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
                {
                    ref FNonPlayerCharacterData npc = ref _npcManager.GetNpcData(i);

                    if (npc.DefinitionID == 0)
                        continue;

                    Vector3 npcPos  = npc.Position;

                    // On the client during forward-prediction ticks, the snapshot position is
                    // frozen at the last confirmed server tick.  Extrapolate using the replicated
                    // per-tick velocity so the predicted NPC position better matches where the
                    // server will have it, reducing correction pops at high ping.
                    if (!kcc.Runner.IsServer && kcc.Runner.IsForward)
                    {
                        int forwardTicks = (int)kcc.Runner.Tick - (int)kcc.Runner.LatestServerTick;
                        if (forwardTicks > 0)
                            npcPos += npc.Velocity * (forwardTicks * kcc.Runner.DeltaTime);
                    }

                    // Closest point on the KCC capsule segment to the NPC centre.
                    Vector3 closest = ClosestPointOnSegment(capsuleBottom, capsuleTop, npcPos);

                    // Direction from NPC centre toward the closest point on the KCC capsule.
                    Vector3 delta  = closest - npcPos;
                    float   distSq = delta.sqrMagnitude;

                    if (distSq >= combinedSq)
                        continue;   // No overlap.

                    float dist = distSq > 1e-8f ? Mathf.Sqrt(distSq) : 0f;

                    // Build an XZ-only push direction (top-down game, no vertical push).
                    Vector3 pushDir;
                    if (dist > 1e-4f)
                    {
                        pushDir = new Vector3(delta.x / dist, 0f, delta.z / dist);
                    }
                    else
                    {
                        // Degenerate: player and NPC centres are coincident.
                        // Use a stable arbitrary horizontal direction as a fallback.
                        pushDir = Vector3.right;
                    }

                    float pushMagSq = pushDir.sqrMagnitude;
                    if (pushMagSq < 1e-8f)
                    {
                        // Directly above or below — no horizontal component, pick world X.
                        pushDir = Vector3.right;
                    }
                    else
                    {
                        pushDir /= Mathf.Sqrt(pushMagSq);
                    }

                    float overlap = combined - dist;
                    data.TargetPosition += pushDir * overlap;

                    anyPenetration = true;

                    // Update endpoints immediately so later NPCs in this same pass
                    // see the corrected player position.
                    capsuleBottom = data.TargetPosition + new Vector3(0f, kccRadius, 0f);
                    capsuleTop    = data.TargetPosition + new Vector3(0f, kccHeight - kccRadius, 0f);
                }

                if (!anyPenetration)
                    break;
            }
        }

        // PRIVATE METHODS

        /// <summary>Returns the point on segment AB that is closest to point P.</summary>
        private static Vector3 ClosestPointOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab      = b - a;
            float   abSqMag = ab.sqrMagnitude;
            if (abSqMag < 1e-8f)
                return a;

            float t = Vector3.Dot(p - a, ab) / abSqMag;
            return a + Mathf.Clamp01(t) * ab;
        }
    }
}
