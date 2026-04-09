// using DWD.Pooling; // TODO: Port from LichLord
// using DWD.Utility.Loading; // TODO: Port from LichLord
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterSpawner : MonoBehaviour
    {
        public Action<FNonPlayerCharacterSpawnParams, NonPlayerCharacter> OnSpawned;

        public void SpawnNPC(ref FNonPlayerCharacterData data, int index)
        {
            NonPlayerCharacterDefinition definition = NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn NPC with invalid definition, id: " + data.DefinitionID);
                return;
            }

            var spawnParams = new FNonPlayerCharacterSpawnParams
            {
                Index = index,
                DefinitionId = data.DefinitionID,
                Position = data.Position,
                Rotation = data.Rotation,
                TeamId = data.TeamID,
            };

            // TODO: Port asset bundle loading from LichLord
            // For now, use direct prefab instantiation
            GameObject prefab = definition.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning("Cannot spawn NPC - no prefab assigned for definition: " + definition.Name);
                return;
            }

            GameObject instance = UnityEngine.Object.Instantiate(prefab, spawnParams.Position, spawnParams.Rotation);
            NonPlayerCharacter spawnedNPC = instance.GetComponent<NonPlayerCharacter>();

            if (spawnedNPC == null)
            {
                Debug.LogWarning("NPC prefab is missing NonPlayerCharacter component!");
                UnityEngine.Object.Destroy(instance);
                return;
            }

            OnSpawned?.Invoke(spawnParams, spawnedNPC);
        }
    }
}
