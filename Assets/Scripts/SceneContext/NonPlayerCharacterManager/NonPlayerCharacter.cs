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
        private NonPlayerCharacterRuntimeState _runtimeState;
        public NonPlayerCharacterRuntimeState RuntimeState => _runtimeState;

        private NonPlayerCharacterReplicator _replicator;
        public NonPlayerCharacterReplicator Replicator => _replicator;

        private NonPlayerCharacterDefinition _definition;
        public NonPlayerCharacterDefinition Definition => _definition;

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
