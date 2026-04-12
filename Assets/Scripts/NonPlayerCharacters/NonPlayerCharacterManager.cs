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

        [Header("Player-NPC Separation – Push Strength")]
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Fraction of the overlap resolved per server tick (0 = no push, 1 = instant full " +
                 "separation). Values below 1 smooth the push over several ticks and reduce the " +
                 "perceived snap when latency is high.  Server-side only.")]
        private float _pushStrength = 1.0f;

        [Header("Client-Side Predictive Separation")]
        [SerializeField]
        [Range(1f, 30f)]
        [Tooltip("Speed (units/sec) at which the visual separation offset decays back toward the " +
                 "server-authoritative NPC position once the server has acknowledged the push. " +
                 "Higher values track the network position more tightly; lower values give a " +
                 "softer, more gradual blend-out.")]
        private float _separationDecaySpeed = 10f;

        [SerializeField]
        [Range(0.1f, 5f)]
        [Tooltip("Maximum time (seconds) an NPC can stay in the predictive HELD state before " +
                 "its entry is released so it can return to its server-authoritative position. " +
                 "Prevents NPCs from appearing frozen at the player boundary when the player is " +
                 "standing still.  The NPC will re-enter HELD from the correct direction on the " +
                 "following frame if it is still inside the separation circle.")]
        private float _heldReleaseTimeout = 0.5f;

        // Minimum squared magnitude used when checking whether a computed push vector is
        // effectively zero (avoids normalising near-zero vectors).
        private const float EPSILON_SQUARED = 1e-8f;

        // Minimum XZ distance at which we consider two centres non-coincident and can
        // derive a reliable push direction from their delta.
        private const float DISTANCE_EPSILON = 1e-4f;

        // Squared distance threshold used to decide when the client display position has
        // converged closely enough to the network position that tracking can stop.
        // (0.01 units ² ≈ 1 cm round-trip, imperceptible at game scale.)
        private const float CONVERGENCE_THRESHOLD_SQUARED = 1e-4f;

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [Networked]
        protected int _dataCount { get; set; }

        private NonPlayerCharacterSpawner _spawner = new NonPlayerCharacterSpawner();

        private Dictionary<int, NPCViewEntry> _views = new Dictionary<int, NPCViewEntry>(NonPlayerCharacterConstants.MAX_NPC_REPS);

        // Per-NPC display positions persisted across render frames for smooth client-side
        // separation.  Only entries for NPCs that are currently being held or decayed are stored;
        // NPCs in the FREE state (network pos already outside player circle, no offset) have no
        // entry here.  Cleared when a view is returned so recycled NPC indices start fresh.
        // Initial capacity: at ~150 ms RTT only NPCs within ~1 step of the player require
        // tracking — roughly 16 slots covers the common case without over-allocating.
        private readonly Dictionary<int, Vector3> _npcDisplayPositions = new Dictionary<int, Vector3>(16);

        // Per-NPC accumulated time (seconds) spent continuously in the HELD state.
        // Reset whenever the NPC exits HELD into DECAYING (server push acknowledged).
        // When the accumulated time exceeds _heldReleaseTimeout the entry is removed so
        // the NPC can return to its server-authoritative position rather than staying
        // frozen at the boundary edge indefinitely.
        private readonly Dictionary<int, float> _npcHeldTime = new Dictionary<int, float>(16);
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

        /// <summary>
        /// Visual-only separation pass run on clients each render frame.
        ///
        /// <para>
        /// The previous single-frame approach just pushed NPC transforms away from the
        /// local player after interpolation, but the push was thrown away at the start of
        /// the next frame when <c>OnRender</c> reset the position to the new network-
        /// interpolated value.  When the player is moving forward, the interpolation can
        /// carry an NPC from <em>in front of</em> the player to <em>behind</em> it in one
        /// lerp step (because the server pushed relative to an older player position),
        /// causing a visible flicker/pop.
        /// </para>
        ///
        /// <para>
        /// This version maintains a persistent display position per NPC and operates as a
        /// three-state machine:
        /// </para>
        /// <list type="bullet">
        ///   <item><b>FREE</b> – network position is already outside the player circle and
        ///   there is no outstanding offset.  The NPC transform simply follows the network
        ///   data; no entry is stored in <see cref="_npcDisplayPositions"/>.</item>
        ///   <item><b>HELD</b> – network position is inside the player circle (server push
        ///   hasn't arrived yet).  The display position is clamped to the circle edge in
        ///   the <em>same direction</em> as the last displayed position, preventing the
        ///   interpolation from pulling the NPC through the player boundary.  A per-NPC
        ///   timer (<see cref="_npcHeldTime"/>) releases the entry after
        ///   <see cref="_heldReleaseTimeout"/> seconds so NPCs return to their
        ///   server-authoritative positions when the player is stationary — without the
        ///   timer, the HELD direction feeds from itself and locks the NPC to the wrong
        ///   edge indefinitely.</item>
        ///   <item><b>DECAYING</b> – network position has exited the circle (server has
        ///   processed the push).  The display position smoothly moves toward the
        ///   authoritative network position at <see cref="_separationDecaySpeed"/>, but is
        ///   never allowed to cross back inside the circle while converging.  Entering
        ///   DECAYING resets the HELD timer.</item>
        /// </list>
        ///
        /// <para>
        /// No <see cref="FNonPlayerCharacterData"/> fields are mutated; all adjustments are
        /// purely cosmetic and scoped to the NPC's <c>Transform</c>.
        /// </para>
        /// </summary>
        private void ApplyPredictiveClientSeparation()
        {
            PlayerCharacter localPlayer = Context?.LocalPlayerCharacter;
            if (localPlayer == null)
                return;

            float combined   = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth;
            float combinedSq = combined * combined;

            Vector3 playerPos = localPlayer.transform.position;
            float   dt        = Time.deltaTime;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                int       key          = pair.Key;
                Transform npcTransform = entry.NPC.CachedTransform;

                // networkPos: where OnRender just placed the NPC via snapshot interpolation.
                Vector3 networkPos = npcTransform.position;

                // XZ distance from the local player to the server-replicated NPC position.
                float ndx           = networkPos.x - playerPos.x;
                float ndz           = networkPos.z - playerPos.z;
                float networkDistSq = ndx * ndx + ndz * ndz;

                bool    hasLastDisplay = _npcDisplayPositions.TryGetValue(key, out Vector3 lastDisplayPos);
                Vector3 displayPos;

                if (networkDistSq < combinedSq)
                {
                    // ── HELD ──────────────────────────────────────────────────────────────
                    // The server data places the NPC inside the player circle; the
                    // authoritative push hasn't reached us yet.  Pin the display position
                    // on the circle edge in the direction of the last displayed position so
                    // the NPC cannot lerp through the player as the interpolation catches up.
                    //
                    // Timeout: if the NPC has been continuously held for longer than
                    // _heldReleaseTimeout, release the entry so the NPC can return to its
                    // server position.  This prevents NPCs from staying frozen at the
                    // boundary when the player is stationary (the HELD direction is derived
                    // from lastDisplayPos which feeds into itself, permanently locking the
                    // angle until the entry is cleared).  After release the NPC shows at its
                    // network position for one frame, then re-enters HELD from the correct
                    // direction on the following frame if still inside the circle.
                    _npcHeldTime.TryGetValue(key, out float heldTime);
                    heldTime += dt;
                    if (heldTime >= _heldReleaseTimeout)
                    {
                        _npcDisplayPositions.Remove(key);
                        _npcHeldTime.Remove(key);
                        // OnRender already set the transform to networkPos; leave it there.
                        continue;
                    }
                    _npcHeldTime[key] = heldTime;

                    Vector3 pushDir;

                    if (hasLastDisplay)
                    {
                        float ldx     = lastDisplayPos.x - playerPos.x;
                        float ldz     = lastDisplayPos.z - playerPos.z;
                        float ldistSq = ldx * ldx + ldz * ldz;

                        if (ldistSq > EPSILON_SQUARED)
                            pushDir = new Vector3(ldx, 0f, ldz) * (1f / Mathf.Sqrt(ldistSq));
                        else
                            pushDir = networkDistSq > EPSILON_SQUARED
                                ? new Vector3(ndx, 0f, ndz) * (1f / Mathf.Sqrt(networkDistSq))
                                : Vector3.right;
                    }
                    else
                    {
                        // First frame this NPC is overlapping: use the network direction.
                        pushDir = networkDistSq > EPSILON_SQUARED
                            ? new Vector3(ndx, 0f, ndz) * (1f / Mathf.Sqrt(networkDistSq))
                            : Vector3.right;
                    }

                    displayPos = new Vector3(
                        playerPos.x + pushDir.x * combined,
                        networkPos.y,
                        playerPos.z + pushDir.z * combined);
                }
                else if (hasLastDisplay)
                {
                    // ── DECAYING ──────────────────────────────────────────────────────────
                    // The server has pushed the NPC outside the circle.  Move the display
                    // position back toward the authoritative network position at a fixed
                    // speed, but clamp it to stay outside the circle the whole way so the
                    // NPC never visually re-enters the player during convergence.
                    //
                    // Reset the HELD timer: the server acknowledged the push, so the NPC
                    // is no longer "waiting" for authoritative separation.
                    _npcHeldTime.Remove(key);

                    displayPos = Vector3.MoveTowards(lastDisplayPos, networkPos, _separationDecaySpeed * dt);

                    float ddx         = displayPos.x - playerPos.x;
                    float ddz         = displayPos.z - playerPos.z;
                    float displayDistSq = ddx * ddx + ddz * ddz;

                    if (displayDistSq < combinedSq)
                    {
                        float   ddist    = displayDistSq > EPSILON_SQUARED ? Mathf.Sqrt(displayDistSq) : 0f;
                        Vector3 clampDir = ddist > DISTANCE_EPSILON
                            ? new Vector3(ddx / ddist, 0f, ddz / ddist)
                            : Vector3.right;
                        displayPos = new Vector3(
                            playerPos.x + clampDir.x * combined,
                            networkPos.y,
                            playerPos.z + clampDir.z * combined);
                    }

                    // Once we've converged with the network position, exit tracking so
                    // future frames skip the dictionary lookup entirely for this NPC.
                    if ((displayPos - networkPos).sqrMagnitude < CONVERGENCE_THRESHOLD_SQUARED)
                    {
                        _npcDisplayPositions.Remove(key);
                        npcTransform.position = networkPos;
                        continue;
                    }
                }
                else
                {
                    // ── FREE ──────────────────────────────────────────────────────────────
                    // Network position is already outside the circle with no outstanding
                    // offset.  Nothing to do; OnRender has already placed the NPC correctly.
                    continue;
                }

                _npcDisplayPositions[key] = displayPos;
                npcTransform.position     = displayPos;
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

            // === 5. Predictive client-side separation ===
            // Apply a visual-only push of NPC transforms away from the locally-predicted
            // player position.  This runs only on non-authority clients and compensates
            // for the ~RTT delay before the server-authoritative separation arrives.
            if (!hasAuthority)
                ApplyPredictiveClientSeparation();

            _viewCount = fromDataCount;
        }

        private void ReturnView(int index, NPCViewEntry entry)
        {
            _npcDisplayPositions.Remove(index);
            _npcHeldTime.Remove(index);

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
