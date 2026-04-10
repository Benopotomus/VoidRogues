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

        [Serializable]
        public struct FNPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        private NonPlayerCharacterSpawner _spawner = new NonPlayerCharacterSpawner();

        [SerializeField]
        private FNPCLoadState[] _loadStates;

        public FNPCLoadState[] LoadStates => _loadStates;

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

            _loadStates = new FNPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _loadStates[i] = new FNPCLoadState();
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i);
                _localRuntimeStates[i].CopyData(ref _npcDatas.GetRef(i));
            }

            if (verboseLogging)
                Debug.Log($"[NPC Manager] Initialized {_loadStates.Length} NPC slots");
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

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                ref FNonPlayerCharacterData networkData = ref _npcDatas.GetRef(i);
                if (!_localRuntimeStates[i].Data.IsEqual(ref networkData))
                    _localRuntimeStates[i].CopyData(ref networkData);
            }
        }

        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            if (verboseLogging)
                Debug.Log($"[NPC Manager] NPC GameObject Spawned! Index: {spawnParams.Index} | Position: {spawnParams.Position} | NPC: {character.name}");

            ref FNPCLoadState loadState = ref _loadStates[spawnParams.Index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;

            _localRuntimeStates[spawnParams.Index].SetPosition(spawnParams.Position);

            bool hasAuthority = Runner.IsServer || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;

            character.OnSpawned(_localRuntimeStates[spawnParams.Index], this, hasAuthority, tick);

            OnCharacterSpawned?.Invoke(character);
        }

        public ref FNonPlayerCharacterData GetNpcData(int index)
        {
            return ref _npcDatas.GetRef(index);
        }

        public NonPlayerCharacter GetNpc(int index)
        {
            if (_loadStates[index].LoadState != ELoadState.Loaded)
                return null;
            return _loadStates[index].NPC;
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

            Vector3 viewPosition = localPlayerCharacter.transform.position;
            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            bool hasAuthority = HasStateAuthority || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i, tick);
                var renderStateData = renderState.Data;
                bool shouldBeActive = renderState.IsActive();
                
                ref FNPCLoadState loadState = ref _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Requesting spawn for NPC slot {i} (was None)");

                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref renderStateData, i);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnRender(renderState, hasAuthority, renderDeltaTime, tick);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    if (verboseLogging)
                        Debug.Log($"[NPC Manager] Despawning NPC at index {i}");

                    DespawnNPCGameObject(i);
                }
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

        private void DespawnNPCGameObject(int index)
        {
            ref FNPCLoadState loadState = ref _loadStates[index];

            if (loadState.LoadState == ELoadState.Loaded)
            {
                if (verboseLogging)
                    Debug.Log($"[NPC Manager] Starting recycle/despawn for NPC at index {index} | NPC: {loadState.NPC?.name}");

                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
                loadState.NPC = null;

                // OnCharacterDespawned?.Invoke(...); // You can re-enable if needed
            }
        }
    }
}