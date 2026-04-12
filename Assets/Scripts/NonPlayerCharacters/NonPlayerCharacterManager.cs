using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [Header("Debug Settings")]
        [SerializeField]
        [Tooltip("Enable detailed logging for NPC spawning, loading, and state changes")]
        private bool verboseLogging = false;

        [Header("Player-NPC Separation")]
        [SerializeField]
        [Tooltip("NPC collision radius used for server-side player-NPC separation (world units). " +
                 "Should match the FollowerEntity shape.radius on the NPC prefab.")]
        private float _npcSeparationRadius = 0.5f;

        [SerializeField]
        [Tooltip("Player collision radius used for server-side player-NPC separation (world units). " +
                 "Should match the KCC Radius on the PlayerCharacter prefab.")]
        private float _playerSeparationRadius = 0.35f;

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }

        private NonPlayerCharacterSpawner _spawner = new NonPlayerCharacterSpawner();

        private Dictionary<int, NPCViewEntry> _views = new Dictionary<int, NPCViewEntry>(NonPlayerCharacterConstants.MAX_NPC_REPS);
        private List<int> _finishedViews = new List<int>(NonPlayerCharacterConstants.MAX_NPC_REPS); // For cleanup
        private int _viewCount;

        // Pre-allocated list for player lookups in the separation pass (avoids per-tick allocation).
        private readonly List<PlayerCharacter> _playerSearchList = new List<PlayerCharacter>(4);

        public Action<NonPlayerCharacter> OnCharacterSpawned;
        public Action<NonPlayerCharacter> OnCharacterDespawned;

        private ArrayReader<FNonPlayerCharacterData> _dataBufferReader;
        protected PropertyReader<int> _dataCountReader;

        public override void Spawned()
        {
            base.Spawned();

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Spawned on {(Runner.IsServer ? "Server" : "Client")} | Mode: {Runner.GameMode}");

            _spawner.OnPrefabSpawned += OnNPC_Loaded;

            _dataBufferReader = GetArrayReader<FNonPlayerCharacterData>(nameof(_npcDatas));
            _dataCountReader = GetPropertyReader<int>(nameof(_dataCount));

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Initialized {NonPlayerCharacterConstants.MAX_NPC_REPS} NPC slots");
        }

        private FNonPlayerCharacterData CreateNPCData(Vector3 spawnPos,
              NonPlayerCharacterDefinition definition,
              ENPCSpawnType spawnType,
              ETeamID teamID,
              EAttitude attitude)
        {
            if (verboseLogging)
                Debug.Log($"[NPC Manager] Creating NPC data | Definition: {definition.name} | Type: {spawnType} | Team: {teamID} | Attitude: {attitude}");

            FNonPlayerCharacterData data = new FNonPlayerCharacterData
            {
                DefinitionID = definition.TableID,
                SpawnType = spawnType,
                Position = spawnPos,
                Rotation = Quaternion.identity
            };

            var dataDefinition = definition.GetDataDefinition(spawnType);
            if (dataDefinition != null)
                dataDefinition.InitializeData(ref data, definition, spawnType, teamID, attitude);

            return data;
        }

        private int SpawnNPC(ref FNonPlayerCharacterData data)
        {
            if (!Runner.IsServer && Runner.GameMode != GameMode.Single)
            {
                if (verboseLogging)
                    Debug.LogWarning($"[NPC Manager] SpawnNPC called without authority! (IsServer: {Runner.IsServer}, GameMode: {Runner.GameMode})");
                return -1;
            }

            int freeIndex = GetFreeIndex();
            if (freeIndex == -1)
            {
                if (verboseLogging)
                    Debug.LogWarning("[NPC Manager] No free NPC index available! Max capacity reached.");
                return -1;
            }

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Spawning NPC at index {freeIndex} | Position: {data.Position}");

            _npcDatas.Set(freeIndex, data);
            _dataCount++;

            return freeIndex;
        }

        public int SpawnNPC(Vector3 spawnPos,
            NonPlayerCharacterDefinition definition,
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            if (!HasStateAuthority)
            {
                if (verboseLogging)
                    Debug.LogWarning($"[NPC Manager] SpawnNPC called on non-authority object! (HasStateAuthority: false)");
                return -1;
            }

            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, spawnType, teamID, attitude);
            return SpawnNPC(ref data);
        }

        public void SpawnNPCInvader(Vector3 spawnPos,
            NonPlayerCharacterDefinition definition,
            ETeamID teamID,
            EAttitude attitude,
            int formationIndex)
        {
            if (!HasStateAuthority)
            {
                if (verboseLogging)
                    Debug.LogWarning("[NPC Manager] SpawnNPCInvader called without StateAuthority!");
                return;
            }

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Spawning Invader | FormationIndex: {formationIndex}");

            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, ENPCSpawnType.Invader, teamID, attitude);
            var invaderData = definition.GetDataDefinition(ENPCSpawnType.Invader) as InvaderDataDefinition;

            if (invaderData == null)
            {
                Debug.LogError("Trying to spawn a non-invader as an invader");
                return;
            }

            invaderData.SetFormationIndex(formationIndex, ref data);
            SpawnNPC(ref data);
        }

        private void OnNPC_Loaded(FNonPlayerCharacterData data, int index, NonPlayerCharacter character)
        {
            if (verboseLogging)
                Debug.Log($"[NPC Manager] NPC GameObject Spawned! Index: {index} | Position: {data.Position}");

            // If the view entry was removed before the async load completed, discard the instantiated object.
            if (!_views.TryGetValue(index, out var entry))
            {
                character.StartRecycle();
                return;
            }

            entry.NPC = character;
            entry.LoadState = ELoadState.Loaded;

            bool hasAuthority = Runner.IsServer || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;
            character.OnSpawned(ref data, this, hasAuthority, tick); 
        }

        public ref FNonPlayerCharacterData GetNpcData(int index)
        {
            return ref _npcDatas.GetRef(index);
        }

        public NonPlayerCharacter GetNpc(int index)
        {
            if (_views.TryGetValue(index, out var entry) && entry.LoadState == ELoadState.Loaded)
                return entry.NPC;
            return null;
        }

        public int GetFreeIndex()
        {
            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_npcDatas.GetRef(i).DefinitionID == 0)
                    return i;
            }
            return -1;
        }


        // FIXED UPDATE NETWORK
        public override void FixedUpdateNetwork()
        {
            if (!Context.IsGameplayActive())
                return;

            bool hasAuthority = HasStateAuthority || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;
            float invDeltaTime = Runner.DeltaTime > 1e-8f ? 1f / Runner.DeltaTime : 0f;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                ref FNonPlayerCharacterData data = ref _npcDatas.GetRef(pair.Key);

                if (hasAuthority)
                {
                    // Capture position before the update to compute a per-tick velocity.
                    // The velocity is replicated so clients can extrapolate NPC positions during
                    // forward-prediction ticks, reducing player-correction pops.
                    Vector3 prevPos = data.Position;
                    entry.NPC.OnFixedUpdateNetwork(ref data, tick, hasAuthority);
                    data.Velocity = (data.Position - prevPos) * invDeltaTime;
                }
                else
                {
                    entry.NPC.OnFixedUpdateNetwork(ref data, tick, hasAuthority);
                }
            }

            // Server-side separation: push NPC positions (and their FollowerEntity transforms) away
            // from players. This ensures the replicated _npcDatas positions already reflect
            // player-NPC boundaries, so clients see NPCs spread away from the player in snapshots
            // and NPCDepenetrationProcessor reads consistent, overlap-free positions.
            if (hasAuthority)
                ApplyPlayerNpcSeparation();
        }

        // RENDER UPDATE
        // RENDER - Cleaned up to match ProjectilePool style
        public override void Render()
        {
            base.Render();
            if (!Context.IsGameplayActive())
                return;

            bool hasAuthority = HasStateAuthority;
            float renderTime = hasAuthority ? Runner.LocalRenderTime : Runner.RemoteRenderTime;
            float localDeltaTime = Time.deltaTime;
            float networkDeltaTime = Runner.DeltaTime;
            int tick = Runner.Tick;

            if (TryGetSnapshotsBuffers(out var fromNetworkBuffer, out var toNetworkBuffer, out float bufferAlpha) == false)
                return;

            NetworkArrayReadOnly<FNonPlayerCharacterData> fromDataBuffer = _dataBufferReader.Read(fromNetworkBuffer);
            NetworkArrayReadOnly<FNonPlayerCharacterData> toDataBuffer = _dataBufferReader.Read(toNetworkBuffer);
            int fromDataCount = _dataCountReader.Read(fromNetworkBuffer);
            int toDataCount = _dataCountReader.Read(toNetworkBuffer);

            // === 1. Remove mispredicted / extra views ===
            for (int i = fromDataCount; i < _viewCount; i++)
            {
                if (_views.TryGetValue(i, out var viewEntry))
                {
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Removing mispredicted view at index {i}");

                    ReturnView(i, viewEntry);
                    _views.Remove(i);
                }
            }

            // === 2. Spawn missing views ===
            for (int i = _viewCount; i < fromDataCount; i++)
            {
                int bufferIndex = i % NonPlayerCharacterConstants.MAX_NPC_REPS;
                var data = fromDataBuffer[bufferIndex];

                if (_views.ContainsKey(i))
                    continue;

                if (verboseLogging)
                    Debug.Log($"[NPC Manager] Requesting spawn for NPC view index {i}");
                
                var newEntry = new NPCViewEntry { LoadState = ELoadState.Loading };
                _views.Add(i, newEntry);

                _spawner.SpawnNPC(ref data, i);   // Note: passing global view index i
            }

            // === 3. Update all current views ===
            _finishedViews.Clear();

            int bufferLength = NonPlayerCharacterConstants.MAX_NPC_REPS;
            int minDataKey = fromDataCount - bufferLength;   // similar logic to projectiles

            foreach (var pair in _views)
            {
                var entry = pair.Value;
                int key = pair.Key;

                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                if (key >= minDataKey)
                {
                    int bufferIndex = key % bufferLength;
                    var toData = toDataBuffer[bufferIndex];
                    var fromData = fromDataBuffer[bufferIndex];

                    entry.NPC.OnRender(ref toData, ref fromData, bufferAlpha, renderTime, networkDeltaTime, localDeltaTime, tick, hasAuthority);
                    entry.LastData = toData;
                }
                else
                {
                    // Data fell out of ring buffer → use last known data
                    entry.NPC.OnRender(ref entry.LastData, ref entry.LastData, 0f, renderTime, networkDeltaTime, localDeltaTime, tick, hasAuthority);
                }

                if(entry.LoadState != ELoadState.Loaded)
                    _finishedViews.Add(key);
                
            }

            // === 4. Cleanup finished views ===
            for (int i = 0; i < _finishedViews.Count; i++)
            {
                int key = _finishedViews[i];
                if (_views.TryGetValue(key, out var entry))
                {
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Despawning finished NPC at index {key}");

                    ReturnView(key, entry);
                    _views.Remove(key);
                }
            }

            _viewCount = fromDataCount;
        }

        private void ReturnView(int index, NPCViewEntry entry)
        {
            if (entry.NPC != null)
            {
                var npc = entry.NPC;
                npc.StartRecycle();
                OnCharacterDespawned?.Invoke(npc);
            }
        }

        /// <summary>
        /// Server-only pass that pushes each active NPC away from every player whose position
        /// overlaps the combined (NPC + player) separation radius.
        ///
        /// Both <c>_npcDatas[i].Position</c> (replicated to clients) and the NPC's actual
        /// Transform (read by FollowerEntity in the next Unity Update) are updated, so:
        /// <list type="bullet">
        ///   <item>Clients receive snapshot positions that already reflect player-NPC separation,
        ///         making <see cref="NPCDepenetrationProcessor"/> reads consistent across peers.</item>
        ///   <item>FollowerEntity continues pathfinding from the pushed position, producing a
        ///         natural "crowd parts around the player" appearance on the host.</item>
        /// </list>
        /// </summary>
        private void ApplyPlayerNpcSeparation()
        {
            Runner.GetAllBehaviours(_playerSearchList);
            if (_playerSearchList.Count == 0)
                return;

            float combinedRadius = _npcSeparationRadius + _playerSeparationRadius;
            float combinedRadiusSq = combinedRadius * combinedRadius;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                ref FNonPlayerCharacterData data = ref _npcDatas.GetRef(pair.Key);
                if (data.DefinitionID == 0)
                    continue;

                Vector3 npcPos = data.Position;
                bool pushed = false;

                for (int p = 0; p < _playerSearchList.Count; p++)
                {
                    PlayerCharacter player = _playerSearchList[p];
                    if (player == null)
                        continue;

                    Vector3 playerPos = player.transform.position;
                    float dx = npcPos.x - playerPos.x;
                    float dz = npcPos.z - playerPos.z;
                    float distSq = dx * dx + dz * dz;

                    if (distSq >= combinedRadiusSq)
                        continue;

                    float dist = distSq > 1e-8f ? Mathf.Sqrt(distSq) : 0f;
                    float overlap = combinedRadius - dist;

                    Vector3 pushDir;
                    if (dist > 1e-4f)
                        pushDir = new Vector3(dx / dist, 0f, dz / dist);
                    else
                        pushDir = Vector3.right; // degenerate: coincident centres

                    npcPos += pushDir * overlap;
                    pushed = true;
                }

                if (pushed)
                {
                    data.Position = npcPos;
                    // Keep FollowerEntity in sync so it continues from the pushed position,
                    // giving it a chance to route around the player on the next Update frame.
                    entry.NPC.CachedTransform.position = npcPos;
                }
            }
        }

        private class NPCViewEntry
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
            public FNonPlayerCharacterData LastData;   // Used when data rolls out of the ring buffer
        }
    }
}
