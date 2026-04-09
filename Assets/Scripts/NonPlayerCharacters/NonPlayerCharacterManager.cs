using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterManager : ContextBehaviour
    {
        [SerializeField] private NonPlayerCharacterReplicator _replicatorPrefab;
        [SerializeField] private NonPlayerCharacterDefinition[] _definitionTable;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float raycastLength = 6f;

        private List<NonPlayerCharacterReplicator> _replicators = new List<NonPlayerCharacterReplicator>();

        public Action<NonPlayerCharacter> OnCharacterSpawned;
        public Action<NonPlayerCharacter> OnCharacterDespawned;

        public void AddReplicator(NonPlayerCharacterReplicator replicator)
        {
            if (!_replicators.Contains(replicator))
            {
                _replicators.Add(replicator);
            }
        }

        public override void Spawned()
        {
            // Initialize the static definition table for lookups.
            if (_definitionTable != null)
            {
                NonPlayerCharacterTable.Initialize(_definitionTable);
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

            NonPlayerCharacterReplicator replicator = GetReplicatorWithFreeSlots();
            if (replicator == null)
                return -1;

            int freeLocalIndex = replicator.GetFreeIndex();
            if (freeLocalIndex == -1)
                return -1;

            int fullIndex = freeLocalIndex + (replicator.Index * NonPlayerCharacterConstants.MAX_NPC_REPS);
            replicator.SpawnNPC(ref data, freeLocalIndex);
            return fullIndex;
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


        public NonPlayerCharacterReplicator GetReplicatorForIndex(int replicatorIndex)
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.Index == replicatorIndex)
                    return replicator;
            }

            var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity, null,
                                onBeforeSpawned: (runner, obj) =>
                                {
                                    var r = obj.GetComponent<NonPlayerCharacterReplicator>();
                                    r.Index = (byte)replicatorIndex;
                                }
            );

            if (newReplicator != null)
            {
                AddReplicator(newReplicator);
                return newReplicator;
            }

            return null;
        }

        public NonPlayerCharacterReplicator GetReplicatorWithFreeSlots()
        {
            foreach (var replicator in _replicators)
            {
                if (replicator.HasFreeIndex())
                    return replicator;
            }

            if (_replicators.Count < NonPlayerCharacterConstants.MAX_REPLICATORS)
            {
                var newReplicator = Runner.Spawn(_replicatorPrefab, Vector3.zero, Quaternion.identity, null,
                                    onBeforeSpawned: (runner, obj) =>
                                    {
                                        var r = obj.GetComponent<NonPlayerCharacterReplicator>();
                                        r.Index = (byte)_replicators.Count;
                                    }
                );

                if (newReplicator != null)
                {
                    AddReplicator(newReplicator);
                    return newReplicator;
                }
            }

            Debug.Log("No replicator with free slots found");
            return null;
        }

        public void DespawnAllInvaders()
        {
            if (!HasStateAuthority)
                return;

            foreach (var replicator in _replicators)
            {
                replicator.DespawnInvaders();
            }
        }

        public void SetInvaderAttitude(EAttitude newAttitude)
        {
            if (!HasStateAuthority)
                return;

            foreach (var replicator in _replicators)
            {
                replicator.SetInvaderAttitude(newAttitude);
            }
        }

        public FNonPlayerCharacterData GetNpcDataAtIndex(int fullIndex)
        {
            int localIndex = fullIndex % NonPlayerCharacterConstants.MAX_NPC_REPS;
            int replicatorIndex = fullIndex / NonPlayerCharacterConstants.MAX_NPC_REPS;

            if (_replicators.Count <= replicatorIndex)
                return new FNonPlayerCharacterData();

            return _replicators[replicatorIndex].GetNpcData(localIndex);
        }

        public NonPlayerCharacterRuntimeState GetNpcRuntimeStateAtIndex(int fullIndex)
        {
            int localIndex = fullIndex % NonPlayerCharacterConstants.MAX_NPC_REPS;
            int replicatorIndex = fullIndex / NonPlayerCharacterConstants.MAX_NPC_REPS;

            if (_replicators.Count <= replicatorIndex)
                return null;

            return _replicators[replicatorIndex].GetNpcRuntimeState(localIndex);
        }

        public NonPlayerCharacter GetNpcAtIndex(int fullIndex)
        {
            int localIndex = fullIndex % NonPlayerCharacterConstants.MAX_NPC_REPS;
            int replicatorIndex = fullIndex / NonPlayerCharacterConstants.MAX_NPC_REPS;

            if (_replicators.Count <= replicatorIndex)
                return null;

            return _replicators[replicatorIndex].GetNpc(localIndex);
        }
    }
}
