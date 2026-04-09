using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Stateless AI helper called by <see cref="NonPlayerCharacterManager"/> each simulation tick
    /// on the host.
    ///
    /// Each NPC:
    ///  1. Checks for nearby players to start interaction.
    ///  2. Wanders within its designated radius when idle.
    ///  3. Faces the interacting player when in dialogue.
    /// </summary>
    public static class NPCAI
    {
        /// <summary>
        /// Advances a single NPC's state by one simulation tick.
        /// </summary>
        public static NPCState Tick(
            NPCState          state,
            Vector2           spawnOrigin,
            List<Vector2>     playerPositions,
            List<int>         playerIndices,
            NPCDefinition[]   database,
            float             deltaTime,
            int               currentTick)
        {
            if (!state.IsActive)
                return state;

            if (database == null || state.TypeIndex >= database.Length)
                return state;

            var def = database[state.TypeIndex];

            // 1. Check for player interaction.
            state = UpdateInteraction(state, playerPositions, playerIndices, def);

            // 2. If interacting, face the player and stay put.
            if (state.InteractingPlayer >= 0)
            {
                state.Velocity  = Vector2.zero;
                state.AnimState = 2; // Talk
                return state;
            }

            // 3. Wander behaviour.
            state = UpdateWander(state, spawnOrigin, def, deltaTime, currentTick);

            return state;
        }

        private static NPCState UpdateInteraction(
            NPCState        state,
            List<Vector2>   playerPositions,
            List<int>       playerIndices,
            NPCDefinition   def)
        {
            float rangeSq = def.InteractionRange * def.InteractionRange;

            // If already interacting, check if player moved out of range.
            if (state.InteractingPlayer >= 0)
            {
                bool stillInRange = false;
                for (int i = 0; i < playerPositions.Count; i++)
                {
                    if (playerIndices[i] == state.InteractingPlayer)
                    {
                        float sq = (playerPositions[i] - state.Position).sqrMagnitude;
                        if (sq <= rangeSq * 1.5f) // Slightly larger range to prevent flicker
                        {
                            stillInRange = true;
                        }
                        break;
                    }
                }

                if (!stillInRange)
                {
                    state.InteractingPlayer = -1;
                    state.DialogueState     = 0; // None
                    state.AnimState         = 0; // Idle
                }

                return state;
            }

            // Look for the nearest player within interaction range.
            float nearestSq  = float.MaxValue;
            int   nearestIdx = -1;

            for (int i = 0; i < playerPositions.Count; i++)
            {
                float sq = (playerPositions[i] - state.Position).sqrMagnitude;
                if (sq < rangeSq && sq < nearestSq)
                {
                    nearestSq  = sq;
                    nearestIdx = playerIndices[i];
                }
            }

            if (nearestIdx >= 0)
            {
                state.InteractingPlayer = nearestIdx;
                state.DialogueState     = 1; // Greeting
            }

            return state;
        }

        private static NPCState UpdateWander(
            NPCState       state,
            Vector2        spawnOrigin,
            NPCDefinition  def,
            float          deltaTime,
            int            currentTick)
        {
            // Pick a new wander target if the interval has elapsed or we reached the current one.
            bool needNewTarget = (currentTick - state.WanderStartTick) >= def.WanderIntervalTicks;
            float distToTarget = Vector2.Distance(state.Position, state.WanderTarget);

            if (needNewTarget || distToTarget < 0.15f)
            {
                // Use prime-number multipliers for deterministic pseudo-random angle and
                // radius distribution based on the current tick and NPC type.
                float angle  = ((currentTick * 7919 + state.TypeIndex * 104729) % 36000) / 100f * Mathf.Deg2Rad;
                float radius = def.WanderRadius * (((currentTick * 6271 + state.TypeIndex * 48611) % 1000) / 1000f);

                state.WanderTarget    = spawnOrigin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                state.WanderStartTick = currentTick;
            }

            // Move towards the wander target.
            Vector2 direction = state.WanderTarget - state.Position;
            float   dist      = direction.magnitude;

            if (dist > 0.15f)
            {
                Vector2 velocity = direction.normalized * def.MoveSpeed;
                state.Velocity = velocity;
                state.Position += velocity * deltaTime;
                state.AnimState = 1; // Walk
            }
            else
            {
                state.Velocity  = Vector2.zero;
                state.AnimState = 0; // Idle
            }

            return state;
        }
    }
}
