using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterDataDefinition : ScriptableObject
    {

        // Config
        protected const int DEFINITION_BITS = 8;          // 0–255
        protected const int DEFINITION_SHIFT = 0;
        protected const byte DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;

        protected const int SPAWN_TYPE_BITS = 3;          // 0–7
        protected const int SPAWN_TYPE_SHIFT = DEFINITION_SHIFT + DEFINITION_BITS;
        protected const byte SPAWN_TYPE_MASK = (1 << SPAWN_TYPE_BITS) - 1;

        protected const int TEAM_BITS = 3;                // 0–7
        protected const int TEAM_SHIFT = SPAWN_TYPE_SHIFT + SPAWN_TYPE_BITS;
        protected const byte TEAM_MASK = (1 << TEAM_BITS) - 1;

        // 19 total

        // Condition (byte)
        protected const int NPC_STATE_BITS = 4;           // 0–15
        protected const int ANIMATION_INDEX_BITS = 2;     // 0–3
        protected const int ADDITIVE_HIT_REACT_BITS = 2;     // 0–3

        protected const int NPC_STATE_SHIFT = 0;
        protected const int ANIMATION_INDEX_SHIFT = NPC_STATE_SHIFT + NPC_STATE_BITS;
        protected const int ADDITIVE_HIT_REACT_SHIFT = ANIMATION_INDEX_SHIFT + ANIMATION_INDEX_BITS;

        protected const byte NPC_STATE_MASK = (1 << NPC_STATE_BITS) - 1;
        protected const byte ANIMATION_INDEX_MASK = (1 << ANIMATION_INDEX_BITS) - 1;
        protected const byte ADDITIVE_HIT_REACT_MASK = (1 << ADDITIVE_HIT_REACT_BITS) - 1;

        // Attitude (byte)
        protected const int ATTITUDE_BITS = 8;              // 0–255
        protected const int ATTITUDE_SHIFT = 0;
        protected const byte ATTITUDE_MASK = (1 << ATTITUDE_BITS) - 1;

        public virtual void InitializeData(ref FNonPlayerCharacterData npcData,
            NonPlayerCharacterDefinition definition,
            ENPCSpawnType spawnType,
            ETeamID teamID,
            EAttitude attitude)
        {
            // Initialize Configuration
            npcData.Configuration = 0;
            SetDefinitionID(definition.TableID, ref npcData);
            SetSpawnType(spawnType, ref npcData);
            SetTeamID(teamID, ref npcData);

            // Initialize Condition
            npcData.Condition = 0;
            SetState(ENPCState.Idle, ref npcData);
            SetAnimationIndex(0, ref npcData);
            SetAdditiveHitReactIndex(0, ref npcData);

            // Initialize Attitude
            npcData.Attitude = 0;
            SetAttitude(attitude, ref npcData);
        }

        // DefinitionID
        public int GetDefinitionID(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Configuration >> DEFINITION_SHIFT) & DEFINITION_MASK;
        }

        public void SetDefinitionID(int definitionIndex, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            definitionIndex = Mathf.Clamp(definitionIndex, 0, DEFINITION_MASK);
            config = (config & ~(DEFINITION_MASK << DEFINITION_SHIFT)) | (definitionIndex << DEFINITION_SHIFT);
            npcData.Configuration = config;
        }

        // Spawn Type
        public ENPCSpawnType GetSpawnType(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCSpawnType)((npcData.Configuration >> SPAWN_TYPE_SHIFT) & SPAWN_TYPE_MASK);
        }

        public void SetSpawnType(ENPCSpawnType newSpawnType, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int spawnType = Mathf.Clamp((int)newSpawnType, 0, SPAWN_TYPE_MASK);
            config = (config & ~(SPAWN_TYPE_MASK << SPAWN_TYPE_SHIFT)) | (spawnType << SPAWN_TYPE_SHIFT);
            npcData.Configuration = config;
        }

        // TeamID
        public virtual ETeamID GetTeamID(ref FNonPlayerCharacterData npcData)
        {
            return (ETeamID)((npcData.Configuration >> TEAM_SHIFT) & TEAM_MASK);
        }

        public virtual void SetTeamID(ETeamID teamID, ref FNonPlayerCharacterData npcData)
        {
            int config = npcData.Configuration;
            int teamValue = Mathf.Clamp((int)teamID, 0, TEAM_MASK);
            config = (ushort)((config & ~(TEAM_MASK << TEAM_SHIFT)) | (teamValue << TEAM_SHIFT));
            npcData.Configuration = config;
        }

        // NPCState
        public ENPCState GetState(ref FNonPlayerCharacterData npcData)
        {
            return (ENPCState)((npcData.Condition >> NPC_STATE_SHIFT) & NPC_STATE_MASK);
        }

        public void SetState(ENPCState newState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));
            npcData.Condition = condition;
        }

        // Animation
        public int GetAnimationIndex(ref FNonPlayerCharacterData npcData)
        {
            return ((npcData.Condition >> ANIMATION_INDEX_SHIFT) & ANIMATION_INDEX_MASK);
        }

        public void SetAnimationIndex(int animationState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int stateValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (stateValue << ANIMATION_INDEX_SHIFT));
            npcData.Condition = condition;
        }

        // Additive Hit React
        public int GetAdditiveHitReactIndex(ref FNonPlayerCharacterData npcData)
        {
            return (npcData.Condition >> ADDITIVE_HIT_REACT_SHIFT) & ADDITIVE_HIT_REACT_MASK;
        }

        public void SetAdditiveHitReactIndex(int additiveHitReactIndex, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;
            int statusValue = Mathf.Clamp((int)additiveHitReactIndex, 0, ADDITIVE_HIT_REACT_MASK);
            condition = (byte)((condition & ~(ADDITIVE_HIT_REACT_MASK << ADDITIVE_HIT_REACT_SHIFT)) | (statusValue << ADDITIVE_HIT_REACT_SHIFT));
            npcData.Condition = condition;
        }

        // Attitude
        public EAttitude GetAttitude(ref FNonPlayerCharacterData npcData)
        {
            int rawValue = (npcData.Attitude >> ATTITUDE_SHIFT) & ATTITUDE_MASK;
            EAttitude attitude = (EAttitude)rawValue;
            return attitude;
        }

        public void SetAttitude(EAttitude newAttitude, ref FNonPlayerCharacterData npcData)
        {
            byte attitude = npcData.Attitude;
            int statusValue = Mathf.Clamp((int)newAttitude, 0, ATTITUDE_MASK);
            attitude = (byte)((attitude & ~(ATTITUDE_MASK << ATTITUDE_SHIFT)) | (statusValue << ATTITUDE_SHIFT));
            npcData.Attitude = attitude;
        }

        // Handle damage application
        public virtual void ApplyDamage(
            ref FNonPlayerCharacterData npcData,
            int damage,
            int hitReactIndex)
        {

        }

        // Health
        public virtual int GetHealth(ref FNonPlayerCharacterData npcData)
        {
            return -1;
        }

        public virtual void SetHealth(int newHealth, ref FNonPlayerCharacterData npcData)
        {

        }

        public void SetStateAndAnimation(ENPCState newState, int animationState, ref FNonPlayerCharacterData npcData)
        {
            byte condition = npcData.Condition;

            // Update State (bits 0–3)
            int stateValue = Mathf.Clamp((int)newState, 0, NPC_STATE_MASK);
            condition = (byte)((condition & ~(NPC_STATE_MASK << NPC_STATE_SHIFT)) | (stateValue << NPC_STATE_SHIFT));

            // Update AnimationIndex (bits 4–5)
            int animValue = Mathf.Clamp(animationState, 0, ANIMATION_INDEX_MASK);
            condition = (byte)((condition & ~(ANIMATION_INDEX_MASK << ANIMATION_INDEX_SHIFT)) | (animValue << ANIMATION_INDEX_SHIFT));

            npcData.Condition = condition;
        }

        // Item

        public virtual FItemData GetCarriedItem(ref FNonPlayerCharacterData npcData)
        {
            return npcData.CarriedItem;
        }

        public virtual void SetCarriedItem(FItemData itemData, ref FNonPlayerCharacterData npcData)
        {
            npcData.CarriedItem = itemData;
        }
    }
}
