using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(menuName = "VoidRogues/NonPlayerCharacters/WorkerDataDefinition")]
    public class WorkerDataDefinition : NonPlayerCharacterDataDefinition
    {
        // Config (19 from base)
        private const int STRONGHOLD_ID_BITS = 4;                // 0–15
        private const int STRONGHOLD_ID_SHIFT = RESERVED_DIALOG_SHIFT + RESERVED_DIALOG_BITS;
        private const byte STRONGHOLD_ID_MASK = (1 << STRONGHOLD_ID_BITS) - 1;
        private const int STRONGHOLD_ID_INVALID = STRONGHOLD_ID_MASK; // 15

        private const int WORKER_INDEX_BITS = 5;                // 0–31
        private const int WORKER_INDEX_SHIFT = STRONGHOLD_ID_SHIFT + STRONGHOLD_ID_BITS;
        private const byte WORKER_INDEX_MASK = (1 << WORKER_INDEX_BITS) - 1;
        private const int WORKER_INDEX_INVALID = WORKER_INDEX_MASK; // 31

        // Events (packed into ushort)
        private const int HEALTH_BITS = 8;               // 0–255
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        private const int HARVEST_PROGRESS_BITS = 4; // 0–15
        private const int HARVEST_PROGRESS_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        private const ushort HARVEST_PROGRESS_MASK = (1 << HARVEST_PROGRESS_BITS) - 1;

        public override void InitializeData(ref FNonPlayerCharacterData npcData,
            NonPlayerCharacterDefinition definition,
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            base.InitializeData(ref npcData, definition, spawnType, teamID, attitude);

            // Initialize Events
            npcData.Events = 0;
            SetHealth(definition.MaxHealth, ref npcData);
        }

        // Worker Stronghold Id
        public int GetStrongholdId(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Configuration >> STRONGHOLD_ID_SHIFT) & STRONGHOLD_ID_MASK);
        }

        public void SetStrongholdId(int strongholdId, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int newValue = Mathf.Clamp((int)strongholdId, 0, STRONGHOLD_ID_MASK);
            config = ((config & ~(STRONGHOLD_ID_MASK << STRONGHOLD_ID_SHIFT)) | (newValue << STRONGHOLD_ID_SHIFT));
            npcData.Configuration = config;
        }

        // Worker Index
        public int GetWorkerIndex(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Configuration >> WORKER_INDEX_SHIFT) & WORKER_INDEX_MASK);
        }

        public void SetWorkerIndex(int workerIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int indexValue = Mathf.Clamp((int)workerIndex, 0, WORKER_INDEX_MASK);
            config = ((config & ~(WORKER_INDEX_MASK << WORKER_INDEX_SHIFT)) | (indexValue << WORKER_INDEX_SHIFT));
            npcData.Configuration = config;
        }

        // Check if the worker data is valid (i.e., not using invalid indices)
        public bool IsValid(ref FNonPlayerCharacterData npcData)
        {
            return GetStrongholdId(ref npcData) != STRONGHOLD_ID_INVALID &&
                   GetWorkerIndex(ref npcData) != WORKER_INDEX_INVALID;
        }

        // Set both StrongholdId and WorkerIndex to their invalid values
        public void SetInvalid(ref FNonPlayerCharacterData npcData)
        {
            SetStrongholdId(STRONGHOLD_ID_INVALID, ref npcData);
            SetWorkerIndex(WORKER_INDEX_INVALID, ref npcData);
        }

        // Health
        public override int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> HEALTH_SHIFT) & HEALTH_MASK;
        }

        public override void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newHealth = Mathf.Clamp(newHealth, 0, HEALTH_MASK);
            events = (ushort)((events & ~(HEALTH_MASK << HEALTH_SHIFT)) | (newHealth << HEALTH_SHIFT));
            npcData.Events = events;
        }

        // Harvest Progress
        public int GetHarvestProgress(ref FNonPlayerCharacterData npcData)
        {
            return (int)((npcData.Events >> HARVEST_PROGRESS_SHIFT) & HARVEST_PROGRESS_MASK);
        }

        public void SetHarvestProgress(int newCurrencyStacks, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            int newStacksCount = Mathf.Clamp((int)newCurrencyStacks, 0, HARVEST_PROGRESS_MASK);
            events = (ushort)((events & ~(HARVEST_PROGRESS_MASK << HARVEST_PROGRESS_SHIFT)) | (newStacksCount << HARVEST_PROGRESS_SHIFT));
            npcData.Events = events;
        }
    }
}
