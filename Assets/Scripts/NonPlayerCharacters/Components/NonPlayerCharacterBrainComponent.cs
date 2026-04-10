// using LichLord.Buildables; // TODO: Port from LichLord
// using LichLord.Items; // TODO: Port from LichLord
// using LichLord.Props; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterBrainComponent : MonoBehaviour
    {
        // TODO: Port IChunkTrackable from LichLord
        public class BrainTarget
        {
            // public IChunkTrackable Target; // TODO: Port from LichLord
            public bool HasTarget;
            public float DistanceToTarget;
        }

        public BrainTarget AttackTarget;
        public BrainTarget HarvestTarget;
        public BrainTarget DepositTarget;
        public BrainTarget NullTarget;

        private NonPlayerCharacter _npc;
        public NonPlayerCharacter NPC => _npc;

        [SerializeField]
        private Vector3 _moveTarget;
        [SerializeField]
        private Vector3 _losTarget;

        private int _updateSensesTick = 32;
        private int _updateDestinationTick = 8;
        private int _updateSpeedTick = 8;
        private int _updateRangesTick = 8;

        [SerializeField]
        private bool _isInActivationRange = false;

        [SerializeField]
        private bool _isInFaceTargetRange = false;

        [SerializeField]
        private ENPCState _activeManeuverState = ENPCState.Inactive;

        [SerializeField] private bool _hasLineOfSight = false;

        [SerializeField]
        private List<NonPlayerCharacterManeuverState> _maneuvers = new List<NonPlayerCharacterManeuverState>();

        private NonPlayerCharacterManeuverState _activeManeuver = null;
        public NonPlayerCharacterManeuverState ActiveManuver => _activeManeuver;

        [SerializeField]
        private PlayerCharacter _targetPlayer;
        public PlayerCharacter TargetPlayer => _targetPlayer;

        [SerializeField]
        private LayerMask _losLayerMask;
        private readonly List<PlayerCharacter> _playerSearchList = new List<PlayerCharacter>(16);

        public void Awake()
        {
            _npc = GetComponent<NonPlayerCharacter>();

            AttackTarget = new BrainTarget();
            AttackTarget.DistanceToTarget = 200f;
            AttackTarget.HasTarget = false;

            HarvestTarget = new BrainTarget();
            HarvestTarget.DistanceToTarget = 200f;
            HarvestTarget.HasTarget = false;

            DepositTarget = new BrainTarget();
            DepositTarget.DistanceToTarget = 200f;
            DepositTarget.HasTarget = false;

            NullTarget = new BrainTarget();
            NullTarget.DistanceToTarget = 200f;
            NullTarget.HasTarget = false;
        }

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, bool hasAuthority)
        {
            _isInActivationRange = false;
            _isInFaceTargetRange = false;
            AttackTarget.HasTarget = false;
            HarvestTarget.HasTarget = false;
            _activeManeuverState = ENPCState.Inactive;
            _moveTarget = Vector3.zero;
            _activeManeuver = null;

            if (hasAuthority)
                FindCurrentTargets();
        }

        public void StartRecycle()
        {
            AttackTarget.HasTarget = false;
            HarvestTarget.HasTarget = false;
            DepositTarget.HasTarget = false;
            _activeManeuver = null;
            _moveTarget = Vector3.zero;
        }

        public void FindCurrentTargets()
        {
            _targetPlayer = null;
            AttackTarget.HasTarget = false;
            AttackTarget.DistanceToTarget = 200f;

            if (_npc == null || _npc.Context == null || _npc.Context.Runner == null)
                return;

            _playerSearchList.Clear();
            _npc.Context.Runner.GetAllBehaviours(_playerSearchList);
            if (_playerSearchList.Count == 0)
                return;

            Vector3 npcPosition = _npc.CachedTransform.position;
            float closestDistanceSqr = float.MaxValue;

            for (int i = 0; i < _playerSearchList.Count; i++)
            {
                var player = _playerSearchList[i];
                if (player == null || player.gameObject == null || !player.gameObject.activeInHierarchy)
                    continue;

                if (player.OwningPlayer != null && !player.OwningPlayer.Statistics.IsAlive)
                    continue;

                float distanceSqr = (player.transform.position - npcPosition).sqrMagnitude;
                if (distanceSqr < closestDistanceSqr)
                {
                    closestDistanceSqr = distanceSqr;
                    _targetPlayer = player;
                }
            }

            if (_targetPlayer == null)
                return;

            AttackTarget.HasTarget = true;
            AttackTarget.DistanceToTarget = Mathf.Sqrt(closestDistanceSqr);
            _moveTarget = _targetPlayer.transform.position;
            _losTarget = _moveTarget;

            if (_npc.Movement != null)
                _npc.Movement.SetMoveTargetPosition(_moveTarget);
        }

        public void AuthorityUpdate(int tick)
        {
            if ((tick % _updateSensesTick) == 0)
                FindCurrentTargets();

            if (_targetPlayer != null && (tick % _updateDestinationTick) == 0)
            {
                _moveTarget = _targetPlayer.transform.position;
                if (_npc != null && _npc.Movement != null)
                    _npc.Movement.SetMoveTargetPosition(_moveTarget);
            }
        }

        public void OnHitFromAnimation()
        {
            // TODO: Port hit event from animation from LichLord
        }

        public void OnSpecialEventFromAnimation()
        {
            // TODO: Port special event from animation from LichLord
        }

        public void OnSweepChangeFromAnimation(bool isSweeping)
        {
            // TODO: Port sweep change from animation from LichLord
        }

        public void SetAnimationForManeuver(ENPCState newState, int animIndex)
        {
            // TODO: Port maneuver animation from LichLord
        }

        public void SetActiveManuever(NonPlayerCharacterManeuverState maneuver)
        {
            _activeManeuver = maneuver;
        }

        public bool HasActiveManeuver()
        {
            return _activeManeuver != null;
        }

        public NonPlayerCharacterManeuverState GetManeuverFromState(ENPCState state)
        {
            for (int i = 0; i < _maneuvers.Count; i++)
            {
                if (_maneuvers[i].ActiveState == state)
                    return _maneuvers[i];
            }
            return null;
        }

        // TODO: Port remaining brain logic from LichLord (AuthorityUpdate, RemoteUpdate, UpdateAuthorityTick, etc.)
    }
}
