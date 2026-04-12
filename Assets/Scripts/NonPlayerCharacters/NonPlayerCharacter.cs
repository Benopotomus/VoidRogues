

namespace VoidRogues.NonPlayerCharacters
{
    using DWD.Pooling;
    using UnityEngine;

    public class NonPlayerCharacter : DWDObjectPoolObject //, IHitTarget, IHitInstigator, IChunkTrackable
    {
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



        public void OnSpawned(ref FNonPlayerCharacterData data, NonPlayerCharacterManager manager, bool hasAuthority, int tick)
        {
            Debug.Log("Spawned");
            _context = manager.Context;
            _manager = manager;
            _healthComponent.OnSpawned(ref data);
            _movementComponent.OnSpawned(ref data, hasAuthority);
            _brainComponent.OnSpawned(ref  data, hasAuthority);
            _spawningComponent.OnSpawned(ref data);
        }

        // Called from NonPlayerCharacterManager.Render() on all peers.
        // Reads interpolated snapshot data – no authority writes happen here.
        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
            float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick, bool hasAuthority)
        {

            _movementComponent.OnRender(ref toData, ref fromData, alpha, renderTime, networkDeltaTime, localDeltaTime, tick, hasAuthority); 
            _animationController.SyncTransformToEntity();
            _animationController.UpdateAnimationEvents();

        }

        // Called from NonPlayerCharacterManager.FixedUpdateNetwork() after the NPC view has fully
        // spawned.  The server runs brain AI and captures the FollowerEntity-driven transform
        // position into the networked struct so clients receive authoritative NPC positions.
        public void OnFixedUpdateNetwork(ref FNonPlayerCharacterData data, int tick, bool hasAuthority)
        {
            if (hasAuthority)
            {
                _brainComponent.AuthorityUpdate(tick);
                _movementComponent.OnFixedNetworkUpdate(ref data, tick);
            }
            else
            {
                // Non-authority peers update destination so their local RVO simulation
                // tracks the same target as the host, keeping movement consistent.
                _brainComponent.RemoteUpdate(tick);
            }
        }

        public void StartRecycle()
        {
            _movementComponent.StartRecycle();
            _brainComponent.StartRecycle();
            _stateComponent.StartRecycle();

            DWDObjectPool.Instance.Recycle(this);
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
