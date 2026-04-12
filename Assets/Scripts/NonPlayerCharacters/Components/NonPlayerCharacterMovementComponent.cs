using Pathfinding;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterMovementComponent : MonoBehaviour
    {
        private NonPlayerCharacter _npc;

        private IAstarAI _follower;
        public IAstarAI AIFollower => _follower;

        [SerializeField] private Vector3 _worldVelocity;
        public Vector3 WorldVelocity => _worldVelocity;

        [SerializeField] private bool _isGrounded;
        public bool IsGrounded => _isGrounded;

        [SerializeField] private LayerMask _layerMask;

        private Vector3 _lastPosition;
        private Vector3 _localVelocity;
        private float _lastYaw;
        private float _yawVelocity;
        public float YawVelocity => _yawVelocity;

        bool _followerUpdatePosition = true;
        bool _followerUpdateRotation = true;
        bool _followerLocalAvoidance = true;
        bool _followerCanMove = true;
        float _followerMaxSpeed = 5f;

        private Transform _cachedTransform;

        private float _teleportDistanceSquared = 36;

        private Vector3 _moveTarget;

        public void OnSpawned(ref FNonPlayerCharacterData data, bool hasAuthority)
        {
            _npc = GetComponent<NonPlayerCharacter>();

            _lastPosition = data.Position;
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
                return;
            }

            if (hasAuthority)
            {
                // Server drives NPC position via FollowerEntity.
                _follower.updatePosition = _followerUpdatePosition;
                _follower.updateRotation = _followerUpdateRotation;
                SetFollowerLocalAvoidance(_followerLocalAvoidance);
            }
            else
            {
                // Clients: OnRender drives the transform from interpolated snapshot data.
                // Disable FollowerEntity movement and RVO so the client-side agent does not
                // diverge from the server-replicated positions.
                _follower.updatePosition = false;
                _follower.updateRotation = false;
                SetFollowerLocalAvoidance(false);
            }

            _follower.simulateMovement = _followerCanMove;
            _follower.maxSpeed = _followerMaxSpeed;
            _follower.Teleport(data.Position, clearPath: true);
        }

        // Called from NonPlayerCharacter.OnFixedUpdateNetwork (server only).
        // Captures the FollowerEntity-driven transform position into the networked struct
        // so clients receive authoritative NPC positions via Fusion replication.
        public void OnFixedNetworkUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            UpdateYawVelocity();
            data.Position = _npc.CachedTransform.position;
        }

        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
            float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick, bool hasAuthority)
        {
            if (hasAuthority)
                return;

            Vector3 fromPosition = fromData.Position;
            Vector3 toPosition = toData.Position;

            if ((fromPosition - toPosition).sqrMagnitude > _teleportDistanceSquared)
            {
                _npc.CachedTransform.position = toPosition;
            }
            else
            {
                _npc.CachedTransform.position = Vector3.Lerp(fromPosition, toPosition, alpha);
            }

            UpdateYawVelocity();
        }

        public void SetMoveTargetPosition(Vector3 newMoveTarget)
        {
            Vector3 delta = _moveTarget - newMoveTarget;
            if (delta.sqrMagnitude < 0.01f)
                return;

            _moveTarget = newMoveTarget;
            if (_follower != null)
            {
                _follower.destination = newMoveTarget;
                if (_follower.canSearch)
                    _follower.SearchPath();
            }
        }

        public void SetFollowerUpdatePosition(bool newEnabled)
        {
            _followerUpdatePosition = newEnabled;
            if (_follower != null)
                _follower.updatePosition = newEnabled;
        }

        public void SetFollowerUpdateRotation(bool newEnabled)
        {
            _followerUpdateRotation = newEnabled;
            if (_follower != null)
                _follower.updateRotation = newEnabled;
        }

        public void SetFollowerCanMove(bool newCanMove)
        {
            _followerCanMove = newCanMove;
            if (_follower != null)
                _follower.simulateMovement = newCanMove;
        }

        public void SetFollowerLocalAvoidance(bool newEnabled)
        {
            _followerLocalAvoidance = newEnabled;
            if (_follower is FollowerEntity fe)
                fe.enableLocalAvoidance = newEnabled;
        }

        public void SetFollowerMaxSpeed(float newSpeed)
        {
            _followerMaxSpeed = newSpeed;
            if (_follower != null)
                _follower.maxSpeed = newSpeed;
        }

        public void OnStateAuthorityChanged(bool hasAuthority)
        {
            SetFollowerUpdatePosition(true);
            SetFollowerUpdateRotation(true);
            SetFollowerCanMove(true);
            SetFollowerLocalAvoidance(hasAuthority);
        }

        public void SetRVOSettings(bool locked, float priority = 0.5f)
        {
            // TODO: Port RVO settings from LichLord (requires FollowerEntity)
        }

        public void StartRecycle()
        {
            SetFollowerUpdatePosition(false);
            SetFollowerUpdateRotation(false);
            SetFollowerCanMove(false);
            SetMoveTargetPosition(Vector3.zero);
            SetFollowerLocalAvoidance(false);
            _follower = null;
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _npc.CachedTransform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
        }
    }
}

