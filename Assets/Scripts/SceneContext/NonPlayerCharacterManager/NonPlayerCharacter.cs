using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Visual representation of a single NPC in the scene.
    ///
    /// Following the LichLord NonPlayerCharacter pattern, this is a local-only
    /// MonoBehaviour (no NetworkObject).
    /// The NonPlayerCharacterReplicator creates one instance per active NPC slot
    /// and drives its state every render frame from the corresponding
    /// NonPlayerCharacterRuntimeState.
    /// </summary>
    public class NonPlayerCharacter : CoreBehaviour
    {
        // Core references
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

        private NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition => _definition;

        // Component references (set via inspector on the NPC prefab)
        [Header("Components")]
        [SerializeField] private NonPlayerCharacterAnimationController _animationController;
        public NonPlayerCharacterAnimationController AnimationController => _animationController;

        [SerializeField] private NonPlayerCharacterBrainComponent _brain;
        public NonPlayerCharacterBrainComponent Brain => _brain;

        [SerializeField] private NonPlayerCharacterMovementComponent _movement;
        public NonPlayerCharacterMovementComponent Movement => _movement;

        [SerializeField] private NonPlayerCharacterStateComponent _state;
        public NonPlayerCharacterStateComponent State => _state;

        [SerializeField] private NonPlayerCharacterHealthComponent _health;
        public NonPlayerCharacterHealthComponent Health => _health;

        [SerializeField] private NonPlayerCharacterHitReactComponent _hitReact;
        public NonPlayerCharacterHitReactComponent HitReact => _hitReact;

        [SerializeField] private NonPlayerCharacterSpawningComponent _spawningComponent;
        public NonPlayerCharacterSpawningComponent SpawningComponent => _spawningComponent;

        [SerializeField] private NonPlayerCharacterAttitudeComponent _attitude;
        public NonPlayerCharacterAttitudeComponent Attitude => _attitude;

        [SerializeField] private NonPlayerCharacterWeaponsComponent _weapons;
        public NonPlayerCharacterWeaponsComponent Weapons => _weapons;

        [SerializeField] private NonPlayerCharacterCarriedItemComponent _carriedItem;
        public NonPlayerCharacterCarriedItemComponent CarriedItem => _carriedItem;

        [SerializeField] private NonPlayerCharacterDialogComponent _dialog;
        public NonPlayerCharacterDialogComponent Dialog => _dialog;

        [SerializeField] private NonPlayerCharacterLifetimeComponent _lifetime;
        public NonPlayerCharacterLifetimeComponent Lifetime => _lifetime;

        [SerializeField] private MeleeHitTrackerComponent _meleeHitTracker;
        public MeleeHitTrackerComponent MeleeHitTracker => _meleeHitTracker;

        [SerializeField] private Collider _collider;
        public new Collider Collider => _collider;

        // Cached transform shortcut (matches LichLord pattern)
        public Transform CachedTransform => transform;

        // Convenience accessors
        public SceneContext Context => _replicator != null ? _replicator.Context : null;
        public int LocalIndex => _runtimeState != null ? _runtimeState.LocalIndex : -1;
        public Vector3 Position => transform.position;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState,
            NonPlayerCharacterReplicator replicator,
            bool hasAuthority,
            int tick)
        {
            _runtimeState = runtimeState;
            _replicator = replicator;
            _definition = runtimeState.Definition;

            transform.position = runtimeState.GetPosition();
            transform.rotation = runtimeState.GetRotation();

            gameObject.name = $"NPC_{runtimeState.FullIndex}_{(_definition != null ? _definition.Name : "Unknown")}";
        }

        public void OnRender(NonPlayerCharacterRuntimeState renderState,
            bool hasAuthority,
            float renderDeltaTime,
            int tick)
        {
            // Position
            transform.position = renderState.GetPosition();
            transform.rotation = renderState.GetRotation();
        }
    }
}
