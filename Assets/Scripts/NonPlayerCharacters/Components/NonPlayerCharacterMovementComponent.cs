using Pathfinding;
using TMPro;
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

        bool _followerUpdatePosition = true;
        bool _followerUpdateRotation = true;
        bool _followerLocalAvoidance = true;
        bool _followerCanMove = true;
        float _followerMaxSpeed = 5f;

        private bool _hasAuthority;

        private Transform _cachedTransform;

        private float _teleportDistanceSquared = 36;

        public void OnSpawned(ref FNonPlayerCharacterData data, bool hasAuthority)
        {
            _npc = GetComponent<NonPlayerCharacter>();
            _hasAuthority = hasAuthority;

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

            if (_hasAuthority)
            {
                // Server/authority: run full pathfinding and RVO simulation.
                _follower.updatePosition = _followerUpdatePosition;
                _follower.updateRotation = _followerUpdateRotation;
                _follower.simulateMovement = _followerCanMove;
            }
            else
            {
                // Non-authority clients: NPC positions come exclusively from network
                // interpolation in OnRender. Disable the FollowerEntity so it does not
                // fight OnRender by applying its own pathfinding/RVO movement, and so
                // it does not corrupt the networked position data during resimulation.
                _follower.updatePosition = false;
                _follower.updateRotation = false;
                _follower.simulateMovement = false;
            }

            _follower.maxSpeed = _followerMaxSpeed;
            _follower.Teleport(data.Position, clearPath: true);
        }

        public void OnFixedNetworkUpdate(ref FNonPlayerCharacterData data, int tick)
        {
            //UpdateVelocity(renderDeltaTime);
            UpdateYawVelocity();
            //_npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);

            // Only the authority (server) should write back the NPC's simulated position.
            // On non-authority clients this write is skipped so that NPCDepenetrationProcessor
            // reads the server-confirmed networked value (restored by Fusion before each
            // resimulated tick) rather than the local render-interpolated transform position.
            // Writing the wrong position here was the root cause of the client prediction
            // mismatch that produced correction pops when RVO moved NPCs on the server.
            if (_hasAuthority)
                data.Position = _npc.CachedTransform.position;
        }

        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
                   float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick, bool hasAuthority)
        { 
            if(hasAuthority)
                return; 

            Vector3 fromPosition = fromData.Position;
            Vector3 toPosition = toData.Position;

            if ((fromPosition - toPosition).sqrMagnitude > _teleportDistanceSquared)
            {
                _npc.CachedTransform.position = toPosition;
            }
            else
            {
                _npc.CachedTransform.position  = Vector3.Lerp(fromPosition, toPosition, alpha);
            }

                //UpdateVelocity(renderDeltaTime);
                UpdateYawVelocity();
            //_npc.AnimationController.UpdateAnimatonForMovement(runtimeState, _localVelocity, _yawVelocity, renderDeltaTime);
        }

        private void UpdateVelocity(float renderDeltaTime)
        {
            _worldVelocity = ((_npc.CachedTransform.position - _lastPosition) / renderDeltaTime);
            _lastPosition = _npc.CachedTransform.position;
            _localVelocity = _npc.CachedTransform.InverseTransformDirection(_worldVelocity);
        }

        private void UpdateYawVelocity()
        {
            Vector3 forward = _npc.CachedTransform.forward;
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
                if (_follower.canSearch)
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
            _hasAuthority = hasAuthority;
            SetFollowerUpdatePosition(hasAuthority);
            SetFollowerUpdateRotation(hasAuthority);
            SetFollowerCanMove(hasAuthority);
        }

        public void SetRVOSettings(bool locked, float priority = 0.5f)
        {
            // TODO: Port RVO settings from LichLord (requires FollowerEntity)
        }
    }
}
