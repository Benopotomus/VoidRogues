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

        private class NPCViewEntry
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        private NonPlayerCharacterSpawner _spawner = new NonPlayerCharacterSpawner();

        private Dictionary<int, NPCViewEntry> _views = new Dictionary<int, NPCViewEntry>(NonPlayerCharacterConstants.MAX_NPC_REPS);

        public Action<NonPlayerCharacter> OnCharacterSpawned;
        public Action<NonPlayerCharacter> OnCharacterDespawned;

        // Prediction
        private Dictionary<int, NonPlayerCharacterRuntimeState> _predictedStates = new Dictionary<int, NonPlayerCharacterRuntimeState>();
        private NonPlayerCharacterRuntimeState[] _localRuntimeStates =
            new NonPlayerCharacterRuntimeState[NonPlayerCharacterConstants.MAX_NPC_REPS];

        public override void Spawned()
        {
            base.Spawned();

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Spawned on {(Runner.IsServer ? "Server" : "Client")} | Mode: {Runner.GameMode}");

            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i);
                _localRuntimeStates[i].CopyData(ref _npcDatas.GetRef(i));
            }

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
            _localRuntimeStates[freeIndex].CopyData(ref data);

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

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            if (verboseLogging)
                Debug.Log($"[NPC Manager] NPC GameObject Spawned! Index: {spawnParams.Index} | Position: {spawnParams.Position}");

            // If the view entry was removed before the async load completed, discard the instantiated object.
            if (!_views.TryGetValue(spawnParams.Index, out var entry))
            {
                character.StartRecycle();
                return;
            }

            entry.NPC = character;
            entry.LoadState = ELoadState.Loaded;

            _localRuntimeStates[spawnParams.Index].SetPosition(spawnParams.Position);

            bool hasAuthority = Runner.IsServer || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;

            character.OnSpawned(_localRuntimeStates[spawnParams.Index], this, hasAuthority, tick);

            OnCharacterSpawned?.Invoke(character);
        }

        // FixedUpdateNetwork – authority/server only.
        // Runs AI decisions and state-machine data writes for every loaded NPC slot.
        // Gated on the NPC view (GameObject) being fully spawned before any logic executes.
        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (!Runner.IsForward || !Runner.IsFirstTick)
                return;

            if (!Context.IsGameplayActive())
                return;

            int tick = Runner.Tick;
            float deltaTime = Runner.DeltaTime;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                UpdateNPCData(i, ref _npcDatas.GetRef(i), tick, deltaTime);
            }
        }

        private void UpdateNPCData(int index, ref FNonPlayerCharacterData data, int tick, float deltaTime)
        {
            // Skip inactive slots.
            if (_localRuntimeStates[index].GetStateFromData(ref data) == ENPCState.Inactive)
                return;

            // Gate: NPC view must be fully spawned before any authority logic runs.
            if (!_views.TryGetValue(index, out var entry) || entry.LoadState != ELoadState.Loaded)
                return;

            entry.NPC.OnFixedUpdateAuthority(ref data, tick, deltaTime);
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

        public bool IsViewLoaded(int index)
        {
            return _views.TryGetValue(index, out var entry) && entry.LoadState == ELoadState.Loaded;
        }

        public NonPlayerCharacterRuntimeState GetNpcRuntimeState(int index)
        {
            if (index >= _localRuntimeStates.Length)
                return null;
            return _localRuntimeStates[index];
        }

        public int GetFreeIndex()
        {
            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_localRuntimeStates[i].GetStateFromData(ref _npcDatas.GetRef(i)) == ENPCState.Inactive)
                    return i;
            }
            return -1;
        }

        public void ReplicateRuntimeState(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (verboseLogging)
                Debug.Log($"[NPC Manager] Replicating runtime state for NPC index {runtimeState.Index}");

            _npcDatas.Set(runtimeState.Index, runtimeState.Data);
        }

        public override void Render()
        {
            base.Render();
            if (!Context.IsGameplayActive())
                return;

            var localPlayerCharacter = Context.LocalPlayerCharacter;
            if (localPlayerCharacter == null)
                return;

            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            bool hasAuthority = HasStateAuthority || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i, tick);
                var renderStateData = renderState.Data;
                bool shouldBeActive = renderState.IsActive();

                bool hasView = _views.TryGetValue(i, out var entry);

                if (shouldBeActive && !hasView)
                {
                    // Slot became active – request a view (async asset bundle load).
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Requesting spawn for NPC slot {i}");

                    _views.Add(i, new NPCViewEntry { LoadState = ELoadState.Loading });
                    _spawner.SpawnNPC(ref renderStateData, i);
                }
                else if (shouldBeActive && hasView && entry.LoadState == ELoadState.Loaded)
                {
                    // View exists and is ready – tick the visual.
                    entry.NPC.OnRender(renderState, hasAuthority, renderDeltaTime, tick);
                }
                else if (!shouldBeActive && hasView && entry.LoadState == ELoadState.Loaded)
                {
                    // Slot became inactive – return the view.
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Despawning NPC at index {i}");

                    ReturnView(i, entry);
                    _views.Remove(i);
                }
                // shouldBeActive && hasView && LoadState == Loading → still loading, nothing to do this frame.
                // !shouldBeActive && !hasView → already clean.
            }
        }

        public NonPlayerCharacterRuntimeState GetRenderState(bool hasAuthority, int index, int tick)
        {
            var localState = _localRuntimeStates[index];

            if (!hasAuthority)
            {
                if (_predictedStates.TryGetValue(index, out var predictedState))
                {
                    if (tick < predictedState.PredictionStartTick)
                        return localState;

                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Using predicted state for NPC index {index}");

                    var predictedStateData = predictedState.Data;
                    predictedStateData.Position = localState.GetPosition();
                    predictedStateData.RawCompressedYaw = localState.GetRawCompressedYaw();
                    predictedState.CopyData(ref predictedStateData);
                    return predictedState;
                }
            }
            return localState;
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
    }
}
