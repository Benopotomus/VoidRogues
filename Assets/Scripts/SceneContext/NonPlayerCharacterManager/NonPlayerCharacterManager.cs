using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Authoritative NPC simulation for VoidRogues.
    ///
    /// Design (LichLord NPC-replicator pattern, adapted for struct-driven networking):
    ///   - A single <see cref="ContextBehaviour"/> owns all NPC state in the scene.
    ///   - State lives in a fixed-size <see cref="NetworkArray{T}"/> of
    ///     <see cref="NPCState"/> structs (up to <see cref="MaxNPCs"/>).
    ///   - AI logic runs on the host only inside <see cref="FixedUpdateNetwork"/>.
    ///   - Clients receive state deltas and update <see cref="NonPlayerCharacter"/>
    ///     visual instances in <see cref="Render"/>.
    ///   - Lives on the GameplayScene / SceneContext hierarchy.
    ///     <see cref="SceneContext.NonPlayerCharacterManager"/> holds the direct reference.
    /// </summary>
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        public const int MaxNPCs = 512;

        [Header("NPC Database")]
        [Tooltip("Index must match NPCState.TypeIndex.")]
        [SerializeField] private NPCDefinition[] _npcDatabase;

        [Header("Prefab")]
        [Tooltip("NonPlayerCharacter prefab instantiated for each active NPC slot.")]
        [SerializeField] private NonPlayerCharacter _npcPrefab;

        [Header("Spawn Configuration")]
        [Tooltip("NPCs to spawn when the scene starts.")]
        [SerializeField] private NPCSpawnPoint[] _spawnPoints;

        // ------------------------------------------------------------------
        // Networked state
        // ------------------------------------------------------------------

        [Networked, Capacity(MaxNPCs)]
        private NetworkArray<NPCState> _npcs { get; }

        // ------------------------------------------------------------------
        // Local state (non-networked)
        // ------------------------------------------------------------------

        private ChangeDetector _changes;

        /// <summary>
        /// Visual instances driven by <see cref="NPCState"/>.
        /// Index matches the <see cref="_npcs"/> array.
        /// </summary>
        private NonPlayerCharacter[] _visuals;

        // Player positions cached per-tick for AI (host only).
        private readonly List<Vector2> _playerPositions = new List<Vector2>(4);
        private readonly List<int>     _playerIndices   = new List<int>(4);

        // Spawn origin positions for each NPC slot (set on activation).
        private Vector2[] _spawnOrigins;

        // Collider-to-index lookup populated each render frame.
        private readonly Dictionary<Collider2D, int> _colliderIndex = new Dictionary<Collider2D, int>();

        private bool _hasSpawned;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _changes      = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _visuals      = new NonPlayerCharacter[MaxNPCs];
            _spawnOrigins = new Vector2[MaxNPCs];

            // Auto-spawn configured NPCs on the host.
            if (Runner.IsServer)
            {
                SpawnConfiguredNPCs();
            }
        }

        // ------------------------------------------------------------------
        // Simulation (host only meaningful path)
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsServer) return;

            CachePlayerPositions();

            for (int i = 0; i < MaxNPCs; i++)
            {
                var state = _npcs[i];
                if (!state.IsActive) continue;

                state = NPCAI.Tick(state, _spawnOrigins[i], _playerPositions, _playerIndices,
                                   _npcDatabase, Runner.DeltaTime, Runner.Tick);
                _npcs.Set(i, state);
            }
        }

        private void CachePlayerPositions()
        {
            _playerPositions.Clear();
            _playerIndices.Clear();

            foreach (var player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out var obj))
                {
                    _playerPositions.Add(obj.transform.position);
                    _playerIndices.Add(player.AsIndex);
                }
            }
        }

        // ------------------------------------------------------------------
        // Presentation (all peers)
        // ------------------------------------------------------------------

        public override void Render()
        {
            _colliderIndex.Clear();

            for (int i = 0; i < MaxNPCs; i++)
            {
                var state = _npcs[i];

                if (state.IsActive)
                {
                    EnsureVisual(i, state.TypeIndex);
                    var npc = _visuals[i];
                    if (npc != null)
                    {
                        npc.gameObject.SetActive(true);
                        npc.ApplyState(state);

                        // Register collider for interaction lookup.
                        var col = npc.GetCollider();
                        if (col != null)
                        {
                            _colliderIndex[col] = i;
                        }
                    }
                }
                else if (_visuals[i] != null)
                {
                    _visuals[i].Deactivate();
                }
            }
        }

        private void EnsureVisual(int index, byte typeIndex)
        {
            if (_visuals[index] != null) return;
            if (typeIndex >= _npcDatabase.Length) return;

            var def = _npcDatabase[typeIndex];

            NonPlayerCharacter npc = null;

            if (_npcPrefab != null)
            {
                npc = Instantiate(_npcPrefab);
            }
            else if (def.VisualPrefab != null)
            {
                var go = Instantiate(def.VisualPrefab);
                npc = go.GetComponent<NonPlayerCharacter>();
                if (npc == null)
                {
                    npc = go.AddComponent<NonPlayerCharacter>();
                }
            }

            if (npc == null) return;

            npc.Initialise(index, def);
            _visuals[index] = npc;
        }

        // ------------------------------------------------------------------
        // Spawn API
        // ------------------------------------------------------------------

        /// <summary>
        /// Spawns all NPCs defined in <see cref="_spawnPoints"/>.
        /// Safe to call multiple times – only the first invocation has an effect.
        /// Called automatically from <see cref="Spawned"/> on the host.
        /// </summary>
        public void SpawnConfiguredNPCs()
        {
            if (_hasSpawned) return;
            _hasSpawned = true;

            if (_spawnPoints == null || _spawnPoints.Length == 0) return;

            foreach (var sp in _spawnPoints)
            {
                ActivateNPC(sp.TypeIndex, sp.Position);
            }

            Debug.Log($"[NonPlayerCharacterManager] Spawned {_spawnPoints.Length} NPC(s).");
        }

        /// <summary>
        /// Activates an NPC slot. Must be called on the host.
        /// </summary>
        public void ActivateNPC(byte typeIndex, Vector2 position)
        {
            if (!Runner.IsServer) return;
            if (typeIndex >= _npcDatabase.Length) return;

            for (int i = 0; i < MaxNPCs; i++)
            {
                if (_npcs[i].IsActive) continue;

                _spawnOrigins[i] = position;

                _npcs.Set(i, new NPCState
                {
                    IsActive          = true,
                    Position          = position,
                    TypeIndex         = typeIndex,
                    AnimState         = 0, // Idle
                    DialogueState     = 0, // None
                    InteractingPlayer = -1,
                    WanderTarget      = position,
                    WanderStartTick   = Runner.Tick,
                });
                return;
            }

            Debug.LogWarning("[NonPlayerCharacterManager] All NPC slots are occupied.");
        }

        /// <summary>
        /// Deactivates an NPC slot. Must be called on the host.
        /// </summary>
        public void DeactivateNPC(int index)
        {
            if (!Runner.IsServer) return;
            if (index < 0 || index >= MaxNPCs) return;

            var state = _npcs[index];
            if (!state.IsActive) return;

            state.IsActive = false;
            _npcs.Set(index, state);
        }

        // ------------------------------------------------------------------
        // Query API
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns the array index for an NPC that owns the given collider, or -1.
        /// </summary>
        public int GetNPCIndexForCollider(Collider2D col)
        {
            return _colliderIndex.TryGetValue(col, out int idx) ? idx : -1;
        }

        /// <summary>
        /// Returns a read-only snapshot of the NPC state at the given index.
        /// </summary>
        public NPCState GetNPCState(int index)
        {
            if (index < 0 || index >= MaxNPCs) return default;
            return _npcs[index];
        }

        /// <summary>
        /// Returns the <see cref="NonPlayerCharacter"/> visual for the given slot,
        /// or null if no visual has been created yet.
        /// </summary>
        public NonPlayerCharacter GetNPCVisual(int index)
        {
            if (index < 0 || index >= MaxNPCs) return null;
            return _visuals != null ? _visuals[index] : null;
        }

        /// <summary>Number of currently active NPCs.</summary>
        public int ActiveNPCCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxNPCs; i++)
                {
                    if (_npcs[i].IsActive) count++;
                }
                return count;
            }
        }

        /// <summary>The NPC definitions database (read-only).</summary>
        public NPCDefinition[] NPCDatabase => _npcDatabase;
    }

    /// <summary>
    /// Inspector-configurable NPC spawn point.
    /// </summary>
    [System.Serializable]
    public class NPCSpawnPoint
    {
        [Tooltip("Index into NonPlayerCharacterManager's NPC database.")]
        public byte TypeIndex;

        [Tooltip("World-space position where the NPC will be placed.")]
        public Vector2 Position;
    }
}
