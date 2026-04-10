

namespace VoidRogues.NonPlayerCharacters
{
    using DWD.Pooling;
    using Fusion;
    using System.Collections.Generic;
    using UnityEngine;

    public class NonPlayerCharacter : DWDObjectPoolObject //, IHitTarget, IHitInstigator, IChunkTrackable
    {
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

        protected NonPlayerCharacterManager _manager;
        public NonPlayerCharacterManager Manager => _manager;

        [SerializeField] private NonPlayerCharacterMovementComponent _movementComponent;
        public NonPlayerCharacterMovementComponent Movement => _movementComponent;

        [SerializeField] private NonPlayerCharacterStateComponent _stateComponent;
        public NonPlayerCharacterStateComponent State => _stateComponent;

        [SerializeField] private NonPlayerCharacterBrainComponent _brainComponent;
        public NonPlayerCharacterBrainComponent Brain => _brainComponent;

        [SerializeField] private NonPlayerCharacterHitReactComponent _hitReactComponent;
        public NonPlayerCharacterHitReactComponent HitReact => _hitReactComponent;

        [SerializeField] private NonPlayerCharacterHealthComponent _healthComponent;
        public NonPlayerCharacterHealthComponent Health => _healthComponent;

        [SerializeField] private NonPlayerCharacterWeaponsComponent _weaponsComponent;
        public NonPlayerCharacterWeaponsComponent Weapons => _weaponsComponent;

        [SerializeField] private NonPlayerCharacterAnimationController _animationController;
        public NonPlayerCharacterAnimationController AnimationController => _animationController;

        [SerializeField] private NonPlayerCharacterSpawningComponent _spawningComponent;
        public NonPlayerCharacterSpawningComponent SpawningComponent => _spawningComponent;

        [SerializeField] private NonPlayerCharacterLifetimeComponent _lifetimeComponent;
        public NonPlayerCharacterLifetimeComponent Lifetime => _lifetimeComponent;

        [SerializeField] private MeleeHitTrackerComponent _meleeHitTracker;
        public MeleeHitTrackerComponent MeleeHitTracker => _meleeHitTracker;

        [SerializeField] private Transform _cachedTransform;
        public Transform CachedTransform => _cachedTransform;

        [SerializeField] private CapsuleCollider _collider;
        public CapsuleCollider Collider => _collider;

        private SceneContext _context;
        public SceneContext Context => _context;

        [SerializeField]
        private int _index;
        public int Index => _index;

        private ETeamID _teamId;
        public ETeamID TeamID => _teamId;

        public Vector3 Position => CachedTransform.position;
        public Vector3 PredictedPosition => _cachedTransform.position + Movement.WorldVelocity;

        public bool IsAttackable
        {
            get
            {
                switch (_stateComponent.CurrentState)
                {
                    case ENPCState.Dead:
                    case ENPCState.Inactive:
                        return false;
                    default:
                        return true;
                }
            }
        }


        [SerializeField]
        private int _squadId;

        [SerializeField]
        private int _formationIndex;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, NonPlayerCharacterManager manager, bool hasAuthority, int tick)
        {
            _runtimeState = runtimeState;
            _context = manager.Context;
            _manager = manager;
            _healthComponent.OnSpawned(runtimeState);
            _movementComponent.OnSpawned(runtimeState, hasAuthority);
            _brainComponent.OnSpawned(runtimeState, hasAuthority);
            _lifetimeComponent.OnSpawned(runtimeState, tick);
            _spawningComponent.OnSpawned(runtimeState);
            _stateComponent.OnSpawned(runtimeState, hasAuthority, tick);
            _animationController.OnSpawned(runtimeState);

           _index = runtimeState.Index;

            Context.NonPlayerCharacterManager.OnCharacterSpawned(this);
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState,
            bool hasAuthority,
            float renderDeltaTime,
            int tick)
        {
            _runtimeState = runtimeState;

            _healthComponent.OnRender(runtimeState, tick);
            _stateComponent.UpdateState(runtimeState, hasAuthority, tick);

            _lifetimeComponent.UpdateLifetime(runtimeState, hasAuthority, tick);
            _animationController.SyncTransformToEntity();
            _animationController.UpdateAnimationEvents();
            _hitReactComponent.UpdateAdditiveHitReactState(runtimeState, tick);
        }

        public void StartRecycle()
        {
         
            _movementComponent.StartRecycle();
            _brainComponent.StartRecycle();
            _stateComponent.StartRecycle();

            DWDObjectPool.Instance.Recycle(this);


            Context.NonPlayerCharacterManager.OnCharacterDespawned(this);
        }

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition GetDefinition(ref FNonPlayerCharacterData data)
        {
            if (data.DefinitionID == 0)
                return null;

            if (_definition == null ||
                _definition.TableID != data.DefinitionID)
            {
                _definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);
            }

            return _definition;
        }

       
    }
}
