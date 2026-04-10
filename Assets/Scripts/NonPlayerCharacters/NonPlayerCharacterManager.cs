using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {

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

            _spawner.OnSpawned += OnNonPlayerCharacterSpawned;
            _loadStates = new FNPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                _loadStates[i] = new FNPCLoadState();
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i);
                _localRuntimeStates[i].CopyData(ref _npcDatas.GetRef(i));
            }
        }

        private FNonPlayerCharacterData CreateNPCData(Vector3 spawnPos,
              NonPlayerCharacterDefinition definition,
              ENPCSpawnType spawnType,
              ETeamID teamID,
              EAttitude attitude)
        {
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
            if (!Runner.IsSharedModeMasterClient && Runner.GameMode != GameMode.Single)
                return -1;

            int freeIndex = GetFreeIndex();
            if (freeIndex == -1)
                return -1;

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
                return -1;

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
                return;

            FNonPlayerCharacterData data = CreateNPCData(spawnPos, definition, ENPCSpawnType.Invader, teamID, attitude);

            var invaderData = definition.GetDataDefinition(ENPCSpawnType.Invader) as InvaderDataDefinition;
            if (invaderData == null)
            {
                Debug.Log("Trying to spawn a non-invader as an invader");
                return;
            }

            invaderData.SetFormationIndex(formationIndex, ref data);

            SpawnNPC(ref data);
        }


        private void OnNonPlayerCharacterSpawned(FNonPlayerCharacterSpawnParams spawnParams, NonPlayerCharacter character)
        {
            ref FNPCLoadState loadState = ref _loadStates[spawnParams.Index];
            loadState.NPC = character;
            loadState.LoadState = ELoadState.Loaded;

            _localRuntimeStates[spawnParams.Index].SetPosition(spawnParams.Position);

            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;

            character.OnSpawned(_localRuntimeStates[spawnParams.Index], this, hasAuthority, tick);
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
            _npcDatas.Set(runtimeState.Index, runtimeState.Data);
        }

        public override void Render()
        {
            base.Render();

            if (!Context.IsGameplayActive())
                return;

            var playerCreature = Context.LocalPlayerCharacter;
            if (playerCreature == null)
                return;

            Vector3 viewPosition = playerCreature.transform.position;
            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i, tick);
                var renderStateData = renderState.Data;

                bool shouldBeActive = renderState.IsActive();

                ref FNPCLoadState loadState = ref _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    _spawner.SpawnNPC(ref renderStateData, i);
                }
                else if (shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    loadState.NPC.OnRender(renderState,
                        hasAuthority,
                        renderDeltaTime,
                        tick);
                }
                else if (!shouldBeActive && loadState.LoadState == ELoadState.Loaded)
                {
                    DespawnNPCGameObject(i);
                }
            }
        }

        public NonPlayerCharacterRuntimeState GetRenderState(bool hasAuthority, int index, int tick)
        {
            var localState = _localRuntimeStates[index];

            // If we are the authority, we dont need to handle prediction
            if (!hasAuthority)
            {
                // Check for predicted data
                if (_predictedStates.TryGetValue(index, out var predictedState))
                {
                    if (tick < predictedState.PredictionStartTick)
                        return localState;

                    //Debug.Log("Using predicted state " + predictedState.GetState() + " Anim: " + predictedState.GetAnimationIndex() + " index: " + index);
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
                loadState.NPC.StartRecycle();
                loadState.LoadState = ELoadState.None;
                loadState.NPC = null;
            }
        }
    }
}
