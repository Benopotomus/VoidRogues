using UnityEngine;

namespace VoidRogues.NPCs
{
    /// <summary>
    /// Reads NPC spawn configuration and activates NPCs in the <see cref="NPCManager"/>
    /// when the scene starts.
    ///
    /// Following the LichLord pattern, the spawner lives on the SceneContext GameObject
    /// (or a child) and is referenced by <see cref="VoidRogues.GameFlow.SceneContext"/>.
    ///
    /// The host calls <see cref="SpawnAll"/> once after the NPCManager is ready.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        [Header("Spawn Configuration")]
        [Tooltip("NPCs to spawn when the scene starts.")]
        [SerializeField] private NPCSpawnPoint[] _spawnPoints;

        [Header("Runtime Spawn")]
        [Tooltip("Default NPC type index used by SpawnAtPosition when no type is specified.")]
        [SerializeField] private byte _defaultTypeIndex;

        private NPCManager _manager;
        private bool       _hasSpawned;

        // ------------------------------------------------------------------
        // API
        // ------------------------------------------------------------------

        /// <summary>
        /// Sets the <see cref="NPCManager"/> reference and spawns all configured NPCs.
        /// Call once from <see cref="VoidRogues.GameFlow.MissionManager"/> or
        /// any host-side initialisation path.
        /// </summary>
        public void Initialise(NPCManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Spawns all NPCs defined in <see cref="_spawnPoints"/>.
        /// Safe to call multiple times – only the first invocation has an effect.
        /// </summary>
        public void SpawnAll()
        {
            if (_hasSpawned) return;
            if (_manager == null)
            {
                Debug.LogWarning("[NPCSpawner] No NPCManager set. Call Initialise() first.");
                return;
            }

            _hasSpawned = true;

            if (_spawnPoints == null || _spawnPoints.Length == 0)
            {
                Debug.Log("[NPCSpawner] No spawn points configured.");
                return;
            }

            foreach (var sp in _spawnPoints)
            {
                _manager.ActivateNPC(sp.TypeIndex, sp.Position);
            }

            Debug.Log($"[NPCSpawner] Spawned {_spawnPoints.Length} NPC(s).");
        }

        /// <summary>
        /// Spawns a single NPC at runtime at the given position.
        /// Host only.
        /// </summary>
        public void SpawnAtPosition(Vector2 position, byte typeIndex)
        {
            if (_manager == null)
            {
                Debug.LogWarning("[NPCSpawner] No NPCManager set.");
                return;
            }

            _manager.ActivateNPC(typeIndex, position);
        }

        /// <summary>
        /// Spawns a single NPC at runtime using the default type index.
        /// </summary>
        public void SpawnAtPosition(Vector2 position)
        {
            SpawnAtPosition(position, _defaultTypeIndex);
        }

        /// <summary>Number of configured spawn points.</summary>
        public int SpawnPointCount => _spawnPoints != null ? _spawnPoints.Length : 0;
    }

    /// <summary>
    /// Inspector-configurable NPC spawn point.
    /// </summary>
    [System.Serializable]
    public class NPCSpawnPoint
    {
        [Tooltip("Index into NPCManager's NPC database.")]
        public byte TypeIndex;

        [Tooltip("World-space position where the NPC will be placed.")]
        public Vector2 Position;
    }
}
