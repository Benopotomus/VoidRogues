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

        [Header("Player-NPC Separation – Push Radius")]
        [SerializeField]
        [Tooltip("Logical radius of the player used for player-NPC overlap tests (world units). " +
                 "Should match the KCC capsule radius on the PlayerCharacter prefab.")]
        private float _playerSeparationRadius = 0.35f;

        [SerializeField]
        [Tooltip("Logical radius of each NPC used for player-NPC overlap tests (world units).")]
        private float _npcSeparationRadius = 0.4f;

        [SerializeField]
        [Tooltip("Extra gap added on top of the combined radius to prevent tight sliding contact.")]
        private float _separationSkinWidth = 0.02f;

        [Header("Player-NPC Separation – Push Strength & Speed")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the overlap resolved per server tick (0 = no push, 1 = instant full " +
                 "separation). Values below 1 smooth the push over several ticks and reduce the " +
                 "perceived snap when latency is high.")]
        private float _pushStrength = 1.0f;

        [SerializeField]
        [Tooltip("Maximum speed (world units per second) at which the client-side predicted NPC " +
                 "visual is pushed away from the local player each render frame. Capping this " +
                 "prevents the visual from over-shooting the authoritative server position while " +
                 "the correction is still in transit, eliminating the warp-and-snap artefact at " +
                 "high latency (≥ 150 ms). Tune alongside _pushStrength; a good starting pair is " +
                 "strength = 0.8, speed = 3.0.")]
        private float _pushSpeed = 3.0f;

        // Minimum squared magnitude used when checking whether a computed push vector is
        // effectively zero (avoids normalising near-zero vectors).
        private const float EPSILON_SQUARED = 1e-8f;

        // Minimum XZ distance at which we consider two centres non-coincident and can
        // derive a reliable push direction from their delta.
        private const float DISTANCE_EPSILON = 1e-4f;

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }

        private NonPlayerCharacterSpawner _spawner = new NonPlayerCharacterSpawner();

        private Dictionary<int, NPCViewEntry> _views = new Dictionary<int, NPCViewEntry>(NonPlayerCharacterConstants.MAX_NPC_REPS);
        private List<int> _finishedViews = new List<int>(NonPlayerCharacterConstants.MAX_NPC_REPS); // For cleanup
        private int _viewCount;

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

            // After all NPC positions are captured for this tick, apply player-NPC
            // separation on the server so NPCs are pushed away from every player
            // (Vampire Survivors style: players walk through enemies freely).
            if (hasAuthority)
                ApplyPlayerNPCSeparation();
        }

        /// <summary>
        /// Iterates every active NPC and pushes it away from any player whose circle
        /// overlaps the NPC's circle on the XZ plane.  Runs server-side only so the
        /// authoritative <c>FNonPlayerCharacterData.Position</c> values are updated and
        /// replicated to all clients.
        ///
        /// The player is intentionally never deflected — only NPCs move.
        /// </summary>
        private void ApplyPlayerNPCSeparation()
        {
            var players = Runner.GetAllBehaviours<PlayerCharacter>();
            if (players == null || players.Count == 0)
                return;

            float combined   = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth;
            float combinedSq = combined * combined;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                ref FNonPlayerCharacterData data = ref _npcDatas.GetRef(pair.Key);
                if (data.DefinitionID == 0)
                    continue;

                Vector3 npcPos    = data.Position;
                Vector3 totalPush = Vector3.zero;

                foreach (PlayerCharacter player in players)
                {
                    if (player == null)
                        continue;

                    Vector3 playerPos = player.transform.position;

                    // XZ-only distance (top-down game; ignore height difference).
                    float dx = npcPos.x - playerPos.x;
                    float dz = npcPos.z - playerPos.z;
                    float distSq = dx * dx + dz * dz;

                    if (distSq >= combinedSq)
                        continue;  // No overlap.

                    float dist    = distSq > EPSILON_SQUARED ? Mathf.Sqrt(distSq) : 0f;
                    float overlap = combined - dist;

                    Vector3 pushDir;
                    if (dist > DISTANCE_EPSILON)
                        pushDir = new Vector3(dx / dist, 0f, dz / dist);
                    else
                        pushDir = Vector3.right;  // Coincident centres — stable fallback.

                    totalPush += pushDir * (overlap * _pushStrength);
                }

                if (totalPush.sqrMagnitude < EPSILON_SQUARED)
                    continue;

                Vector3 newPos = npcPos + totalPush;
                data.Position = newPos;
                entry.NPC.TeleportToPosition(newPos);
            }
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

            // === 5. Client-side predicted NPC push (latency cover) ===
            // The server writes authoritative pushed NPC positions into FNonPlayerCharacterData
            // each tick, but the client must wait one round-trip (~one-way latency) before
            // receiving those updates. At 150 ms one-way latency the client sees NPCs at their
            // un-pushed snapshot positions for several frames while the player walks through
            // them. To eliminate that visual pop, apply the same XZ separation logic here
            // using the local player's current render-time transform position. Only the
            // CachedTransform (visual) is moved — no networked state is written — so the
            // server correction that arrives next converges cleanly rather than causing a
            // position hitch.
            if (!hasAuthority)
                ApplyClientSidePredictedSeparation();
        }

        /// <summary>
        /// Runs on non-authority clients each render frame to visually push NPC transforms
        /// away from the local observed player, covering one-way replication latency (up to
        /// ~150 ms) without waiting for the next server snapshot.
        ///
        /// <b>No network state is written.</b>  Only <c>NonPlayerCharacter.CachedTransform.position</c>
        /// is adjusted.  The server independently applies the same separation each tick and
        /// replicates the corrected positions, so the visual prediction converges smoothly with
        /// the authoritative data.
        ///
        /// The push applied per frame is capped to <c>_pushSpeed * Time.deltaTime</c> world units.
        /// This prevents the visual from over-shooting the authoritative server position while
        /// the correction is still in transit, which was the root cause of the warp-and-snap
        /// artefact seen at ≥ 150 ms latency: the old code teleported by the full overlap every
        /// frame, so when the server snapshot (with the already-pushed position) arrived,
        /// <see cref="Render"/> would reset to the server value and the next frame would
        /// over-push again, creating an oscillation.  By capping the per-frame nudge to a small
        /// velocity-driven amount the client position stays close enough to the authoritative
        /// value that server corrections produce no visible snap.
        /// </summary>
        private void ApplyClientSidePredictedSeparation()
        {
            PlayerCharacter localPlayer = Context?.ObservedPlayerCharacter;
            if (localPlayer == null)
                return;

            Vector3 playerPos  = localPlayer.transform.position;
            float combined     = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth;
            float combinedSq   = combined * combined;

            // Maximum distance this NPC visual may be nudged in a single render frame.
            // Capping by speed * dt keeps the prediction gentle so that arriving server
            // corrections (which already contain the authoritative pushed position) never
            // cause a noticeable snap.
            float maxMoveThisFrame = _pushSpeed * Time.deltaTime;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                Transform npcTransform = entry.NPC.CachedTransform;
                Vector3   npcPos       = npcTransform.position;

                float dx     = npcPos.x - playerPos.x;
                float dz     = npcPos.z - playerPos.z;
                float distSq = dx * dx + dz * dz;

                if (distSq >= combinedSq)
                    continue;

                float dist    = distSq > EPSILON_SQUARED ? Mathf.Sqrt(distSq) : 0f;
                float overlap = combined - dist;

                Vector3 pushDir;
                if (dist > DISTANCE_EPSILON)
                    pushDir = new Vector3(dx / dist, 0f, dz / dist);
                else
                    pushDir = Vector3.right;  // Coincident centres — stable fallback.

                // Apply at most _pushStrength of the overlap, further capped by the
                // per-frame speed budget to avoid fighting the server interpolation.
                float pushAmount = Mathf.Min(overlap * _pushStrength, maxMoveThisFrame);
                npcTransform.position = npcPos + pushDir * pushAmount;
            }
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

        private class NPCViewEntry
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
            public FNonPlayerCharacterData LastData;   // Used when data rolls out of the ring buffer
        }
    }
}
