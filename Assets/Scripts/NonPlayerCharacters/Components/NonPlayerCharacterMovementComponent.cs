using Pathfinding;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterMovementComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

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

        bool _followerUpdatePosition = true;
        bool _followerUpdateRotation = true;
        bool _followerLocalAvoidance = true;
        bool _followerCanMove = true;
        float _followerMaxSpeed = 5f;

        private Transform _cachedTransform;

        private float _teleportDistanceSquared = 36;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority)
        {
            _lastPosition = runtimeState.GetPosition();
            _cachedTransform = transform;
            _follower = GetComponent<IAstarAI>();
            if (_follower == null)
            {
                var followerEntity = GetComponent<FollowerEntity>();
                if (followerEntity == null)
                    followerEntity = gameObject.AddComponent<FollowerEntity>();

                _follower = followerEntity;
            }

            if (_follower != null)
            {
                _follower.updatePosition = _followerUpdatePosition;
                _follower.updateRotation = _followerUpdateRotation;
                _follower.simulateMovement = _followerCanMove;
                _follower.maxSpeed = _followerMaxSpeed;
                _follower.Teleport(runtimeState.GetPosition(), clearPath: true);
            }
        }

        public void AuthorityUpdate(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);
            // TODO: Port TryWriteTransformData from LichLord
        }

        public void RemoteUpdate(NonPlayerCharacterRuntimeState runtimeState, float renderDeltaTime, int tick)
        {
            if (runtimeState.GetState() == ENPCState.Dead || runtimeState.GetState() == ENPCState.Inactive)
                return;

            Vector3 statePosition = runtimeState.GetPosition();

            if ((statePosition - NPC.CachedTransform.position).sqrMagnitude > _teleportDistanceSquared)
            {
                NPC.CachedTransform.position = statePosition;
            }
            else
            {
                Vector3 currentPos = NPC.CachedTransform.position;
                Vector3 targetPos = statePosition;

                float x = Mathf.Lerp(currentPos.x, targetPos.x, renderDeltaTime * 4f);
                float y = Mathf.Lerp(currentPos.y, targetPos.y, renderDeltaTime * 8f);
                float z = Mathf.Lerp(currentPos.z, targetPos.z, renderDeltaTime * 4f);

                NPC.CachedTransform.position = new Vector3(x, y, z);
            }

            float currentYaw = NPC.CachedTransform.eulerAngles.y;
            float targetYaw = runtimeState.GetYaw();

            float lerpedYaw = Mathf.LerpAngle(currentYaw, targetYaw, renderDeltaTime * 10f);

            NPC.CachedTransform.rotation = Quaternion.Euler(0, lerpedYaw, 0);

            UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            _npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);
        }

        private void UpdateVelocity(float renderDeltaTime)
        {
            _worldVelocity = ((NPC.CachedTransform.position - _lastPosition) / renderDeltaTime);
            _lastPosition = NPC.CachedTransform.position;
            _localVelocity = NPC.CachedTransform.InverseTransformDirection(_worldVelocity);
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _cachedTransform.forward;
            float currentYaw = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
            _yawVelocity = currentYaw - _lastYaw;
            _lastYaw = currentYaw;
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
        }

        public void SetFollowerMaxSpeed(float newSpeed)
        {
            _followerMaxSpeed = newSpeed;
            if (_follower != null)
                _follower.maxSpeed = newSpeed;
        }

        private Vector3 _moveTarget;
        public void SetMoveTargetPosition(Vector3 newMoveTarget)
        {
            Vector3 delta = _moveTarget - newMoveTarget;
            if (delta.sqrMagnitude < 0.01f)
                return;

            _moveTarget = newMoveTarget;
            if (_follower != null)
            {
                _follower.destination = newMoveTarget;
                _follower.SearchPath();
            }
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

        public void OnStateAuthorityChanged(bool hasAuthority)
        {
            SetFollowerUpdatePosition(true);
            SetFollowerUpdateRotation(true);
            SetFollowerCanMove(true);
        }

        public void SetRVOSettings(bool locked, float priority = 0.5f)
        {
            // TODO: Port RVO settings from LichLord (requires FollowerEntity)
        }
    }
}
