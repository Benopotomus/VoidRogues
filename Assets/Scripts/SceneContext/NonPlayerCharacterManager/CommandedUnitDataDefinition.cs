using UnityEngine;

// A commanded unit is a player-summoned character.

namespace VoidRogues
{
    [CreateAssetMenu(menuName = "VoidRogues/NonPlayerCharacters/CommandedUnitDataDefinition")]
    public class CommandedUnitDataDefinition : NonPlayerCharacterDataDefinition
    {
        [SerializeField]
        private bool _infiniteLifetime;
        public bool InfiniteLifetime => _infiniteLifetime;

        [SerializeField]
        private int _maxLifetimeProgress = 15;
        public int MaxLifetimeProgress => _maxLifetimeProgress;

        [SerializeField]
        private int _ticksPerLifetimeProgress = 32;
        public int TicksPerLifetimeProgress => _ticksPerLifetimeProgress;

        // Config 
        private const int PLAYER_FOLLOW_BITS = 3;             // 0–8 22
        private const int PLAYER_FOLLOW_SHIFT = RESERVED_DIALOG_SHIFT + RESERVED_DIALOG_BITS;
        private const ushort PLAYER_FOLLOW_MASK = (1 << PLAYER_FOLLOW_BITS) - 1;

        private const int SQUAD_ID_BITS = 2;             // 0–4 24
        private const int SQUAD_ID_SHIFT = PLAYER_FOLLOW_SHIFT + PLAYER_FOLLOW_BITS;
        private const ushort SQUAD_ID_MASK = (1 << SQUAD_ID_BITS) - 1;

        private const int FORMATION_INDEX_BITS = 4;             // 0–15 28
        private const int FORMATION_INDEX_SHIFT = SQUAD_ID_SHIFT + SQUAD_ID_BITS;
        private const ushort FORMATION_INDEX_MASK = (1 << FORMATION_INDEX_BITS) - 1;

        // Events
        private const int HEALTH_BITS = 12;             // 0–4095
        private const int HEALTH_SHIFT = 0;
        private const ushort HEALTH_MASK = (1 << HEALTH_BITS) - 1;

        private const int LIFETIME_PROGRESS_BITS = 4;             // 0–15
        private const int LIFETIME_PROGRESS_SHIFT = HEALTH_SHIFT + HEALTH_BITS;
        private const ushort LIFETIME_PROGRESS_MASK = (1 << LIFETIME_PROGRESS_BITS) - 1;

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
            SetLifetimeProgress(0, ref npcData);
        }

        // Formation
        public int GetSquadId(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> SQUAD_ID_SHIFT) & SQUAD_ID_MASK;
        }

        public void SetSquadId(int squadId, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            squadId = Mathf.Clamp(squadId, 0, SQUAD_ID_MASK);
            config = (config & ~(SQUAD_ID_MASK << SQUAD_ID_SHIFT)) | (squadId << SQUAD_ID_SHIFT);
            npcData.Configuration = config;
        }

        public int GetFormationIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> FORMATION_INDEX_SHIFT) & FORMATION_INDEX_MASK;
        }

        public void SetFormationIndex(int formationIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            formationIndex = Mathf.Clamp(formationIndex, 0, FORMATION_INDEX_MASK);
            config = (config & ~(FORMATION_INDEX_MASK << FORMATION_INDEX_SHIFT)) | (formationIndex << FORMATION_INDEX_SHIFT);
            npcData.Configuration = config;
        }

        // Player
        public int GetPlayerFollowIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> PLAYER_FOLLOW_SHIFT) & PLAYER_FOLLOW_MASK;
        }

        public void SetPlayerFollowIndex(int playerIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            playerIndex = Mathf.Clamp(playerIndex, 0, PLAYER_FOLLOW_MASK);
            config = (config & ~(PLAYER_FOLLOW_MASK << PLAYER_FOLLOW_SHIFT)) | (playerIndex << PLAYER_FOLLOW_SHIFT);
            npcData.Configuration = config;
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

        // Lifetime Progress
        public int GetLifetimeProgress(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Events >> LIFETIME_PROGRESS_SHIFT) & LIFETIME_PROGRESS_MASK;
        }

        public void SetLifetimeProgress(int newProgress, ref FNonPlayerCharacterData npcData)
        {
            ushort events = npcData.Events;
            newProgress = Mathf.Clamp(newProgress, 0, LIFETIME_PROGRESS_MASK);
            events = (ushort)((events & ~(LIFETIME_PROGRESS_MASK << LIFETIME_PROGRESS_SHIFT)) | (newProgress << LIFETIME_PROGRESS_SHIFT));
            npcData.Events = events;
        }
    }
}
