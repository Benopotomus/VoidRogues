using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    public partial class NonPlayerCharacterReplicator : ContextBehaviour, IStateAuthorityChanged
    {
        [SerializeField]
        [Networked]
        public byte Index { get; set; }

        [Serializable]
        public struct FNPCLoadState
        {
            public NonPlayerCharacter NPC;
            public ELoadState LoadState;
        }

        [Networked, Capacity(NonPlayerCharacterConstants.MAX_NPC_REPS)]
        [OnChangedRender(nameof(OnRep_NPCDatas))]
        private NetworkArray<FNonPlayerCharacterData> _npcDatas { get; }

        [SerializeField]
        private FNPCLoadState[] _loadStates;
        public FNPCLoadState[] LoadStates => _loadStates;

        // Prediction
        private Dictionary<int, NonPlayerCharacterRuntimeState> _predictedStates = new Dictionary<int, NonPlayerCharacterRuntimeState>();
        private NonPlayerCharacterRuntimeState[] _localRuntimeStates =
            new NonPlayerCharacterRuntimeState[NonPlayerCharacterConstants.MAX_NPC_REPS];

        [SerializeField] private LayerMask hitMask = ~0; // used to ground npcs on replication

        public override void Spawned()
        {
            base.Spawned();

            Context.NonPlayerCharacterManager.AddReplicator(this);
            _loadStates = new FNPCLoadState[NonPlayerCharacterConstants.MAX_NPC_REPS];

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                int fullIndex = i + (NonPlayerCharacterConstants.MAX_NPC_REPS * Index);
                _loadStates[i] = new FNPCLoadState();
                _localRuntimeStates[i] = new NonPlayerCharacterRuntimeState(this, i, fullIndex);
                _localRuntimeStates[i].CopyData(ref _npcDatas.GetRef(i));
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
        }

        public void StateAuthorityChanged()
        {
            Debug.Log($"StateAuthority Changed, HasStateAuthority: {HasStateAuthority}");
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                if (_loadStates[i].LoadState == ELoadState.Loaded)
                {
                    // Notify NPC of authority change if needed
                }
            }
        }

        public void SpawnNPC(ref FNonPlayerCharacterData data, int index)
        {
            _npcDatas.Set(index, data);
            _localRuntimeStates[index].CopyData(ref data);
        }

        public void ReplicateRuntimeState(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (!HasStateAuthority)
                return;

            _npcDatas.Set(runtimeState.LocalIndex, runtimeState.Data);
        }

        public bool HasFreeIndex()
        {
            return GetFreeIndex() >= 0;
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

        public override void Render()
        {
            base.Render();

            if (!Context.IsGameplayActive())
                return;

            var playerCreature = Context.LocalPlayerCharacter;
            if (playerCreature == null)
                return;

            float renderDeltaTime = Time.deltaTime;
            int tick = Runner.Tick;
            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;

            if (!hasAuthority)
                TimeoutPredictedStates(tick);

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                var renderState = GetRenderState(hasAuthority, i, tick);
                var renderStateData = renderState.Data;

                bool shouldBeActive = renderState.IsActive();

                ref FNPCLoadState loadState = ref _loadStates[i];

                if (shouldBeActive && loadState.LoadState == ELoadState.None)
                {
                    loadState.LoadState = ELoadState.Loading;
                    SpawnNPCGameObject(ref renderStateData, i);
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

        private void SpawnNPCGameObject(ref FNonPlayerCharacterData data, int index)
        {
            var definition = NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);
            if (definition == null || definition.Prefab == null)
            {
                _loadStates[index].LoadState = ELoadState.None;
                return;
            }

            var go = Instantiate(definition.Prefab, data.Position, data.Rotation);
            var npc = go.GetComponent<NonPlayerCharacter>();
            if (npc == null)
            {
                npc = go.AddComponent<NonPlayerCharacter>();
            }

            ref FNPCLoadState loadState = ref _loadStates[index];
            loadState.NPC = npc;
            loadState.LoadState = ELoadState.Loaded;

            _localRuntimeStates[index].SetPosition(data.Position);

            bool hasAuthority = Runner.IsSharedModeMasterClient || Runner.GameMode == GameMode.Single;
            int tick = Runner.Tick;

            npc.OnSpawned(_localRuntimeStates[index], this, hasAuthority, tick);
        }

        int _timeoutPredictionTick = -1;
        private void TimeoutPredictedStates(int tick)
        {
            if (_timeoutPredictionTick == tick)
                return;

            _timeoutPredictionTick = tick;

            // Remove expired states in one go
            var keysToRemove = new List<int>(_predictedStates.Count);

            foreach (var (key, state) in _predictedStates)
            {
                if (tick >= state.PredictionTimeoutTick)
                    keysToRemove.Add(key);
            }

            for (int i = 0; i < keysToRemove.Count; i++)
                _predictedStates.Remove(keysToRemove[i]);
        }

        private void DespawnNPCGameObject(int index)
        {
            ref FNPCLoadState loadState = ref _loadStates[index];
            if (loadState.LoadState == ELoadState.Loaded)
            {
                if (loadState.NPC != null)
                {
                    Destroy(loadState.NPC.gameObject);
                }
                loadState.LoadState = ELoadState.None;
                loadState.NPC = null;
            }
        }

        private void OnRep_NPCDatas()
        {
            int tick = Runner.Tick;
            const int raycastInterval = 1;

            // Cache frequently accessed values
            bool hasStateAuthority = HasStateAuthority;

            if (hasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                ref FNonPlayerCharacterData networkedData = ref _npcDatas.GetRef(i);
                ref var localState = ref _localRuntimeStates[i];

                var newNetworkedState = localState.GetStateFromData(ref networkedData);

                ENPCState oldState = localState.GetState();

                bool needsRaycast = !hasStateAuthority &&
                                   (tick + i) % raycastInterval == 0 &&
                                   localState.GetPosition() != networkedData.Position &&
                                   newNetworkedState != ENPCState.Inactive;

                Vector3 hitPosition = Vector3.zero;
                bool raycastHit = false;

                if (needsRaycast)
                {
                    if (Physics.Raycast(networkedData.Position + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 6f, hitMask))
                    {
                        raycastHit = true;
                        hitPosition = hit.point;
                    }
                }

                bool hasStateChanged = oldState != newNetworkedState;
                localState.CopyData(ref networkedData);

                if (raycastHit)
                {
                    localState.SetPosition(hitPosition);
                }

                if (hasStateChanged &&
                    _predictedStates.TryGetValue(i, out NonPlayerCharacterRuntimeState predictedState))
                {
                    if ((localState.GetState() == predictedState.GetState() &&
                         localState.GetAnimationIndex() == predictedState.GetAnimationIndex()) ||
                        localState.GetState() == ENPCState.Dead)
                    {
                        _predictedStates.Remove(i);
                    }
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

                    var predictedStateData = predictedState.Data;
                    predictedStateData.Position = localState.GetPosition();
                    predictedStateData.RawCompressedYaw = localState.GetRawCompressedYaw();

                    predictedState.CopyData(ref predictedStateData);
                    return predictedState;
                }
            }

            return localState;
        }

        public void DespawnInvaders()
        {
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                NonPlayerCharacterRuntimeState curState = _localRuntimeStates[i];

                if (curState.IsInvader())
                    curState.SetState(ENPCState.Inactive);
            }
        }

        public void SetInvaderAttitude(EAttitude newAttitude)
        {
            if (!HasStateAuthority)
                return;

            for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
            {
                NonPlayerCharacterRuntimeState curState = _localRuntimeStates[i];

                if (curState.IsInvader())
                    curState.SetAttitude(newAttitude);
            }
        }
    }
}
