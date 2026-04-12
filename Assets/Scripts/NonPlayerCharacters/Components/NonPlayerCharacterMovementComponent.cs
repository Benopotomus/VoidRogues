using Pathfinding;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    /// <summary>
    /// Handles NPC locomotion under the "deterministic rollback" model.
    ///
    /// <b>Hard-path design</b><br/>
    /// Rather than letting FollowerEntity move the transform directly (which runs in Unity's
    /// Update loop, outside Fusion's tick loop), we keep FollowerEntity alive only as a
    /// <em>path oracle</em>: it still computes A* paths and provides
    /// <see cref="IAstarAI.steeringTarget"/>, but never writes to the transform.
    ///
    /// The actual position integration happens in
    /// <see cref="NonPlayerCharacterManager.FixedUpdateNetwork"/> Phase 1, which runs on
    /// <em>every peer</em> (server and all clients) during Fusion's tick loop.  Because
    /// Fusion restores the <see cref="FNonPlayerCharacterData"/> array to the correct
    /// historical snapshot before each resimulated tick, both peers compute identical
    /// positions: the stored yaw + isMoving flag are the sole inputs.
    ///
    /// The server updates yaw and isMoving from FollowerEntity every tick in
    /// <see cref="UpdateSteering"/>. Clients get these values via normal Fusion snapshot
    /// replication and never need to run pathfinding.
    ///
    /// As a result <see cref="NPCDepenetrationProcessor"/> reads fully deterministic NPC
    /// positions during KCC resimulation, eliminating the correction pops that occurred
    /// when RVO-moved NPC transforms diverged between server and client.
    /// </summary>
    public class NonPlayerCharacterMovementComponent : MonoBehaviour
    {
        // Distance (squared) beyond which position changes are teleports rather than interpolation.
        private float _teleportDistanceSquared = 36f;

        // Minimum distance to the steering target before the NPC is considered to have arrived.
        private const float ArrivalThreshold = 0.15f;

        private NonPlayerCharacter _npc;
        private IAstarAI _follower;
        private Transform _cachedTransform;

        // Used only for render-side yaw velocity (animation blending).
        private float _lastYaw;
        private float _yawVelocity;
        public float YawVelocity => _yawVelocity;

        private Vector3 _moveTarget;

        public IAstarAI AIFollower => _follower;

        // -------------------------------------------------------------------
        // LIFECYCLE
        // -------------------------------------------------------------------

        public void OnSpawned(ref FNonPlayerCharacterData data, bool hasAuthority)
        {
            _npc = GetComponent<NonPlayerCharacter>();
            _cachedTransform = transform;

            _follower = GetComponent<IAstarAI>();
            if (_follower == null)
            {
                var followerEntity = GetComponent<FollowerEntity>();
                if (followerEntity == null)
                    followerEntity = gameObject.AddComponent<FollowerEntity>();
                _follower = followerEntity;
            }

            if (_follower == null)
            {
                Debug.LogWarning($"[{nameof(NonPlayerCharacterMovementComponent)}] Missing IAstarAI on {name}. NPC pathfinding is disabled.");
            }
            else
            {
                // On all peers: FollowerEntity must never write back to the transform.
                // The transform is driven by FixedUpdateNetwork (simulation) or OnRender (visuals).
                _follower.updatePosition = false;
                _follower.updateRotation = false;

                if (hasAuthority)
                {
                    // Server: keep the path simulation alive so steeringTarget stays fresh.
                    _follower.simulateMovement = true;
                }
                else
                {
                    // Clients: no path queries needed – yaw/isMoving come from the server snapshot.
                    _follower.simulateMovement = false;
                }

                _follower.Teleport(data.Position, clearPath: true);
            }
        }

        public void StartRecycle()
        {
            if (_follower != null)
            {
                _follower.updatePosition = false;
                _follower.updateRotation = false;
                _follower.simulateMovement = false;
                _follower.destination = Vector3.zero;
            }
            _follower = null;
            _moveTarget = Vector3.zero;
        }

        // -------------------------------------------------------------------
        // SIMULATION  (called from NonPlayerCharacter.OnFixedUpdateNetwork)
        // -------------------------------------------------------------------

        /// <summary>
        /// Server-only: reads FollowerEntity's current <see cref="IAstarAI.steeringTarget"/>,
        /// derives a movement yaw angle and updates <see cref="FNonPlayerCharacterData.IsMoving"/>
        /// and <see cref="FNonPlayerCharacterData.Yaw"/> in the networked struct.
        ///
        /// These two values are replicated to clients and used by Phase 1 of
        /// <see cref="NonPlayerCharacterManager.FixedUpdateNetwork"/> on all peers to integrate
        /// position deterministically.
        ///
        /// Must be called <em>after</em> Phase 1 has already integrated
        /// <see cref="FNonPlayerCharacterData.Position"/> so that the transform is synced to the
        /// post-integration position before FollowerEntity reads it.
        /// </summary>
        public void UpdateSteering(ref FNonPlayerCharacterData data, bool hasAuthority)
        {
            if (!hasAuthority)
                return;

            if (_follower == null)
                return;

            // Sync the transform so FollowerEntity's next Update sees the simulation position.
            _cachedTransform.position = data.Position;

            // steeringTarget is the next waypoint produced by A*.
            Vector3 toTarget = _follower.steeringTarget - data.Position;
            toTarget.y = 0f;
            float dist = toTarget.magnitude;

            bool isMoving = dist > ArrivalThreshold;
            data.IsMoving = isMoving;

            if (isMoving)
            {
                // Store the movement direction as a yaw angle (degrees, 0–360).
                // Clients decode this with YawToDirection() in the integration phase.
                Vector3 dir = toTarget / dist;
                data.Yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            }
        }

        // -------------------------------------------------------------------
        // RENDER  (called from NonPlayerCharacter.OnRender)
        // -------------------------------------------------------------------

        /// <summary>
        /// Interpolates the transform position between two confirmed snapshots for visual
        /// smoothness.  Runs on all peers but only moves the transform on non-authority clients;
        /// the server's transform is managed by <see cref="UpdateSteering"/>.
        /// </summary>
        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
            float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick, bool hasAuthority)
        {
            if (hasAuthority)
                return;

            Vector3 fromPosition = fromData.Position;
            Vector3 toPosition = toData.Position;

            _cachedTransform.position = (fromPosition - toPosition).sqrMagnitude > _teleportDistanceSquared
                ? toPosition
                : Vector3.Lerp(fromPosition, toPosition, alpha);

            UpdateYawVelocity();
        }

        // -------------------------------------------------------------------
        // BRAIN INTERFACE  (called from NonPlayerCharacterBrainComponent)
        // -------------------------------------------------------------------

        public void SetMoveTargetPosition(Vector3 newMoveTarget)
        {
            if ((_moveTarget - newMoveTarget).sqrMagnitude < 0.01f)
                return;

            _moveTarget = newMoveTarget;
            if (_follower != null)
            {
                _follower.destination = newMoveTarget;
                if (_follower.canSearch)
                    _follower.SearchPath();
            }
        }

        // -------------------------------------------------------------------
        // STATE COMPONENT INTERFACE  (may be called by NonPlayerCharacterStateComponent)
        // -------------------------------------------------------------------

        public void SetFollowerCanMove(bool newCanMove)
        {
            if (_follower != null)
                _follower.simulateMovement = newCanMove;
        }

        public void SetFollowerUpdatePosition(bool newEnabled)
        {
            // In the deterministic model updatePosition is always false; expose the
            // setter so that commented state-machine code can be re-enabled unchanged.
            if (_follower != null)
                _follower.updatePosition = newEnabled;
        }

        public void SetFollowerUpdateRotation(bool newEnabled)
        {
            if (_follower != null)
                _follower.updateRotation = newEnabled;
        }

        public void SetFollowerLocalAvoidance(bool newEnabled)
        {
            // RVO local-avoidance is not used in the deterministic model.
            // Kept so call-sites in commented state-machine code compile unchanged.
        }

        public void SetFollowerMaxSpeed(float newSpeed)
        {
            if (_follower != null)
                _follower.maxSpeed = newSpeed;
        }

        public void SetRVOSettings(bool locked, float priority = 0.5f)
        {
            // RVO is not used in the deterministic model.
        }

        // -------------------------------------------------------------------
        // HELPERS
        // -------------------------------------------------------------------

        private void UpdateYawVelocity()
        {
            Vector3 forward = _cachedTransform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
        }
    }
}

