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

        // Minimum squared magnitude used when checking whether a computed push vector is
        // effectively zero (avoids normalising near-zero vectors).
        private const float EPSILON_SQUARED = 1e-8f;

        // Minimum XZ distance at which we consider two centres non-coincident and can
        // derive a reliable push direction from their delta.
        private const float DISTANCE_EPSILON = 1e-4f;

        [Header("Client Predictive Separation – Smoothing")]
        [SerializeField]
        [Tooltip("Repulsion force applied per unit of penetration depth when an NPC's display " +
                 "position is inside the exclusion circle (units/s² per unit of overlap). " +
                 "Higher values push NPCs out faster; lower values give a gentler, slower push.")]
        private float _separationPushForce = 40f;

        [SerializeField]
        [Tooltip("Scales the lateral flocking force relative to the main push force. " +
                 "0.1 = flocking nudge is 10 % of the push; increase for more aggressive sideways spreading.")]
        private float _npcFlockingForceScale = 0.1f;

        [SerializeField]
        [Tooltip("Radius within which two pushed NPCs repel each other, " +
                 "causing them to spread sideways instead of piling up (world units).")]
        private float _npcFlockingRadius = 1.2f;

        [SerializeField]
        [Tooltip("Spring stiffness used when reconciling a pushed NPC's display position back to the " +
                 "network position. Higher values snap the NPC back more quickly. Tuned alongside " +
                 "_reconcileSpringDamping for critical damping (no oscillation).")]
        private float _reconcileSpringStrength = 12f;

        [SerializeField]
        [Tooltip("Velocity damping coefficient applied per second during spring reconciliation. " +
                 "Higher values reduce overshoot; lower values allow a small arc before settling. " +
                 "For critical damping use damping ≈ 2 * sqrt(strength).")]
        private float _reconcileSpringDamping = 8f;

        // Squared convergence threshold: when a decaying display position is within this
        // distance² of the network position and velocity is near zero, the entry is removed.
        private const float CONVERGENCE_THRESHOLD_SQUARED         = 0.01f;
        private const float CONVERGENCE_VELOCITY_THRESHOLD_SQUARED = 0.04f;

        // Squared XZ speed below which the local player is considered stationary (units/s)².
        // At ~0.1 u/s or slower the player is not meaningfully moving, so predictive pushes
        // are suppressed and any in-flight display offsets reconcile immediately.
        private const float PLAYER_STATIONARY_SPEED_SQ = 0.01f;

        // Tracks the visual (display) XZ position of each NPC that is currently being
        // pushed by the client predictive separation pass.  Keyed by the same view index
        // used in _views.  Entries exist only while an NPC is inside (or recently exited)
        // the exclusion circle.
        private Dictionary<int, Vector3>  _npcDisplayPositions  = new Dictionary<int, Vector3>(NonPlayerCharacterConstants.MAX_NPC_REPS);

        // XZ display velocity (world units/second) per NPC, maintained across frames so that
        // the spring reconciliation can smoothly blend outward push momentum into the return arc.
        private Dictionary<int, Vector2>  _npcDisplayVelocities = new Dictionary<int, Vector2>(NonPlayerCharacterConstants.MAX_NPC_REPS);

        // Last known XZ player position used to estimate per-frame player velocity.
        private Vector3 _lastPlayerPos;
        private bool    _lastPlayerPosValid;

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
        /// Visual-only separation pass run on non-authority clients each render frame.
        ///
        /// <para>
        /// Uses continuous physics integration — no discrete phases, no position snapping —
        /// so NPCs flow smoothly around the player like water.  A proportional repulsion
        /// force accelerates an NPC's display position out of the exclusion circle while the
        /// server authoritative separation is still travelling across the network.  Once the
        /// server position exits the circle a damped spring pulls the display position back.
        /// RVO-style flocking spreads NPCs sideways so they do not pile up radially.
        /// </para>
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
            {
                _lastPlayerPosValid = false;
                return;
            }

            float combined   = _playerSeparationRadius + _npcSeparationRadius + _separationSkinWidth;
            float combinedSq = combined * combined;
            float dt         = Time.deltaTime;

            Vector3 playerPos = localPlayer.transform.position;

            // Determine whether the player is stationary this frame.
            // When stationary we skip outward pushing entirely so that in-flight
            // display offsets reconcile back to the server positions immediately.
            bool playerIsStationary = true;
            if (_lastPlayerPosValid && dt > 0f)
            {
                float dvx = playerPos.x - _lastPlayerPos.x;
                float dvz = playerPos.z - _lastPlayerPos.z;
                float speedSq = (dvx * dvx + dvz * dvz) / (dt * dt);
                playerIsStationary = speedSq < PLAYER_STATIONARY_SPEED_SQ;
            }
            _lastPlayerPos      = playerPos;
            _lastPlayerPosValid = true;

            float dampFactor = Mathf.Exp(-_reconcileSpringDamping * dt);
            float flockRadSq = _npcFlockingRadius * _npcFlockingRadius;

            foreach (KeyValuePair<int, NPCViewEntry> pair in _views)
            {
                NPCViewEntry entry = pair.Value;
                if (entry.LoadState != ELoadState.Loaded || entry.NPC == null)
                    continue;

                int       key          = pair.Key;
                Transform npcTransform = entry.NPC.CachedTransform;
                Vector3   networkPos   = npcTransform.position;   // interpolated from server snapshots

                float ndx      = networkPos.x - playerPos.x;
                float ndz      = networkPos.z - playerPos.z;
                bool  netInside = (ndx * ndx + ndz * ndz) < combinedSq;

                bool hasDisplay = _npcDisplayPositions.TryGetValue(key, out Vector3 displayPos);
                _npcDisplayVelocities.TryGetValue(key, out Vector2 vel);

                if (!hasDisplay)
                {
                    if (!netInside || playerIsStationary)
                        continue;   // NPC is clear of the circle and not tracked, or player is stationary.

                    // Seed the display position at the network position so force integration
                    // pushes it out smoothly — no snap to the boundary.
                    displayPos = networkPos;
                    vel        = Vector2.zero;
                }

                // ── 1. Repulsion force (display position inside exclusion circle) ────────────
                // Proportional to penetration depth: the deeper the overlap the harder the
                // push, producing a smooth acceleration from rest with no abrupt jump.
                // Skipped when the player is stationary so NPCs reconcile rather than push.
                if (!playerIsStationary)
                {
                float ddx      = displayPos.x - playerPos.x;
                float ddz      = displayPos.z - playerPos.z;
                float dispDist = Mathf.Sqrt(ddx * ddx + ddz * ddz);
                float overlap  = combined - dispDist;
                if (overlap > 0f)
                {
                    float outDirX, outDirZ;
                    if (dispDist > DISTANCE_EPSILON)
                    {
                        float inv = 1f / dispDist;
                        outDirX   = ddx * inv;
                        outDirZ   = ddz * inv;
                    }
                    else
                    {
                        // Display position is exactly on the player.  Use the network→away
                        // direction as the push axis so overlapping NPCs naturally diverge
                        // rather than all snapping to the same arbitrary axis.
                        float ndMag = Mathf.Sqrt(ndx * ndx + ndz * ndz);
                        if (ndMag > DISTANCE_EPSILON)
                        {
                            float inv = 1f / ndMag;
                            outDirX   = ndx * inv;
                            outDirZ   = ndz * inv;
                        }
                        else
                        {
                            outDirX = 1f;
                            outDirZ = 0f;
                        }
                    }

                    float pushMag = overlap * _separationPushForce;
                    vel.x += outDirX * pushMag * dt;
                    vel.y += outDirZ * pushMag * dt;
                }

                // ── 2. RVO-style flocking: spread NPCs sideways ───────────────────────────────
                // Gentle lateral repulsion between NPCs so they fan outward instead of stacking.
                float avoidX = 0f, avoidZ = 0f;
                foreach (KeyValuePair<int, Vector3> otherPair in _npcDisplayPositions)
                {
                    if (otherPair.Key == key)
                        continue;

                    float ex     = displayPos.x - otherPair.Value.x;
                    float ez     = displayPos.z - otherPair.Value.z;
                    float distSq = ex * ex + ez * ez;
                    if (distSq < flockRadSq && distSq > EPSILON_SQUARED)
                    {
                        float dist   = Mathf.Sqrt(distSq);
                        float weight = (_npcFlockingRadius - dist) / _npcFlockingRadius;
                        avoidX += (ex / dist) * weight;
                        avoidZ += (ez / dist) * weight;
                    }
                }

                float flockScale = _separationPushForce * _npcFlockingForceScale;
                vel.x += avoidX * flockScale * dt;
                vel.y += avoidZ * flockScale * dt;
                } // end !playerIsStationary

                // ── 3. Spring reconciliation (server has resolved the separation) ─────────────
                // Pull the display position back toward the network position once the server
                // authoritative push has landed.  When the player is stationary we apply the
                // spring unconditionally so that any in-flight offsets collapse immediately
                // without waiting for the server to confirm the NPC has left the circle.
                if (!netInside || playerIsStationary)
                {
                    vel.x += (networkPos.x - displayPos.x) * _reconcileSpringStrength * dt;
                    vel.y += (networkPos.z - displayPos.z) * _reconcileSpringStrength * dt;
                }

                // ── 4. Velocity damping ────────────────────────────────────────────────────────
                vel.x *= dampFactor;
                vel.y *= dampFactor;

                // ── 5. Integrate position ──────────────────────────────────────────────────────
                displayPos.x += vel.x * dt;
                displayPos.z += vel.y * dt;

                // ── 6. Convergence check (once the server has resolved, or player is stationary) ──
                if (!netInside || playerIsStationary)
                {
                    float ex2 = displayPos.x - networkPos.x;
                    float ez2 = displayPos.z - networkPos.z;
                    if (ex2 * ex2 + ez2 * ez2 < CONVERGENCE_THRESHOLD_SQUARED &&
                        vel.x * vel.x + vel.y * vel.y < CONVERGENCE_VELOCITY_THRESHOLD_SQUARED)
                    {
                        _npcDisplayPositions.Remove(key);
                        _npcDisplayVelocities.Remove(key);
                        continue;
                    }
                }

                _npcDisplayPositions[key] = displayPos;
                _npcDisplayVelocities[key] = vel;
                npcTransform.position      = new Vector3(displayPos.x, networkPos.y, displayPos.z);
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
            if (entry.NPC != null)
            {
                var npc = entry.NPC;
                npc.StartRecycle();
                OnCharacterDespawned?.Invoke(npc);
            }

            // Clear all client-side predictive state so recycled indices start fresh.
            _npcDisplayPositions.Remove(index);
            _npcDisplayVelocities.Remove(index);
        }

        private class NPCViewEntry
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
            public FNonPlayerCharacterData LastData;   // Used when data rolls out of the ring buffer
        }
    }
}
