namespace VoidRogues.NonPlayerCharacters
{
    using UnityEngine;

    public static class NonPlayerCharacterDataUtility
    {
        // Bit size constants
        private const int DEFINITION_BITS = 8;          // 0–255
        private const int DEFINITION_SHIFT = 0;
        private const byte DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;

        private const int SPAWN_TYPE_BITS = 3;          // 0–7
        private const int SPAWN_TYPE_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        private const byte SPAWN_TYPE_MASK = (1 << SPAWN_TYPE_BITS) - 1;

        private const int TEAM_BITS = 3;                // 0–7
        private const int TEAM_SHIFT = SPAWN_TYPE_SHIFT + SPAWN_TYPE_BITS;
        private const byte TEAM_MASK = (1 << TEAM_BITS) - 1;

        // DefinitionID
        public static int GetDefinitionID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> DEFINITION_SHIFT) & DEFINITION_MASK;
        }

        public static void SetDefinitionID(int definitionIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            definitionIndex = Mathf.Clamp(definitionIndex, 0, DEFINITION_MASK);
            config = ((config & ~(DEFINITION_MASK << DEFINITION_SHIFT)) | (definitionIndex << DEFINITION_SHIFT));
            npcData.Configuration = config;
        }

        // Spawn Type
        public static ENPCSpawnType GetSpawnType(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCSpawnType)((npcData.Configuration >> SPAWN_TYPE_SHIFT) & SPAWN_TYPE_MASK);
        }

        public static void SetSpawnType(ENPCSpawnType newSpawnType, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int spawnType = Mathf.Clamp((int)newSpawnType, 0, SPAWN_TYPE_MASK);
            config = (config & ~(SPAWN_TYPE_MASK << SPAWN_TYPE_SHIFT)) | (spawnType << SPAWN_TYPE_SHIFT);
            npcData.Configuration = config;
        }

        // TeamID
        public static ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public static void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }
    }

    // Enums for completeness
    public enum ENPCState : byte
    {
        Inactive,
        Idle,
        Dead,
        HitReact,

        Maneuver_1,
        Maneuver_2,
        Maneuver_3,
        Maneuver_4,

        Maneuver_5,
        Maneuver_6,
        Maneuver_7,
        Maneuver_8,

        Stunned,
        Spawning,
    }

    public enum EAttitude : byte
    {
        None,
        Hostile,
        Passive,
        Defensive,
    }

    public enum ENPCSpawnType : byte
    {
        Invader, // Goes toward the invasion manager's target
        Worker, // Has a worker index for players
        Guard, // Spawns at locations to guard them
        Patrol, // Spawns at patrol points and paths to other ones
        CommandedUnit, // Summoned by the player character and attempts to follow when idle
    }
}
