using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(menuName = "VoidRogues/NonPlayerCharacters/InvaderDataDefinition")]
    public class InvaderDataDefinition : NonPlayerCharacterDataDefinition
    {
        [Header("Formation Offsets")]
        [SerializeField] private Vector3[] _formationOffsets = new Vector3[16];

        // Config 
        private const int FORMATION_BITS = 4;             // 0–15
        private const int FORMATION_SHIFT = DIALOG_INDEX_SHIFT + DIALOG_INDEX_BITS;
        private const ushort FORMATION_MASK = (1 << FORMATION_BITS) - 1;

        // Events
        private const int HEALTH_BITS = 12;             // 0–4095
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

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

        // Invasion NPC
        public bool IsInvasionNPC(ref FNonPlayerCharacterData npcData)
        {
            return true;
        }

        // Formation
        public int GetFormationIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> FORMATION_SHIFT) & FORMATION_MASK;
        }

        public void SetFormationIndex(int formationIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            formationIndex = Mathf.Clamp(formationIndex, 0, FORMATION_MASK);
            config = (config & ~(FORMATION_MASK << FORMATION_SHIFT)) | (formationIndex << FORMATION_SHIFT);
            npcData.Configuration = config;
        }

        public Vector3 GetFormationOffset(ref FNonPlayerCharacterData npcData)
        {
            int index = GetFormationIndex(ref npcData);
            if (index < 0 || index >= _formationOffsets.Length)
                return Vector3.zero;

            return _formationOffsets[index];
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
    }
}
