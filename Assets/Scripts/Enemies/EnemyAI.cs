using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// Stateless AI helper called by <see cref="EnemyManager"/> each simulation tick
    /// on the host.
    ///
    /// Each enemy:
    ///  1. Seeks the nearest player.
    ///  2. Applies separation force to avoid stacking.
    ///  3. Attacks if within range.
    /// </summary>
    public static class EnemyAI
    {
        /// <summary>
        /// Number of array slots to check on each side of the current enemy when
        /// computing separation forces.  Keeps per-tick cost manageable for large groups.
        /// </summary>
        public const int SeparationNeighbourhoodRadius = 32;
        /// <summary>
        /// Advances a single enemy's state by one simulation tick.
        /// </summary>
        public static EnemyState Tick(
            EnemyState      state,
            List<Vector2>   playerPositions,
            EnemyDefinition[] database,
            float           deltaTime,
            float           separationRadius,
            float           separationForce,
            int             selfIndex,
            NetworkArray<EnemyState> allEnemies)
        {
            if (!state.IsActive || state.AnimState == 3) // dead
                return state;

            if (database == null || state.TypeIndex >= database.Length)
                return state;

            var def = database[state.TypeIndex];

            // 1. Find nearest player.
            Vector2 targetPos  = state.Position;
            float   nearestSq  = float.MaxValue;
            int     targetIdx  = -1;

            for (int p = 0; p < playerPositions.Count; p++)
            {
                float sq = (playerPositions[p] - state.Position).sqrMagnitude;
                if (sq < nearestSq)
                {
                    nearestSq = sq;
                    targetPos = playerPositions[p];
                    targetIdx = p;
                }
            }

            state.TargetPlayer = targetIdx;

            if (targetIdx < 0)
            {
                state.AnimState = 0; // Idle
                state.Velocity  = Vector2.zero;
                return state;
            }

            float distToTarget = Mathf.Sqrt(nearestSq);

            // 2. Compute seek direction.
            Vector2 seek = Vector2.zero;
            if (distToTarget > def.AttackRange)
            {
                seek = (targetPos - state.Position).normalized * def.MoveSpeed;
            }

            // 3. Separation from neighbours.
            Vector2 separation = ComputeSeparation(state, selfIndex, allEnemies, separationRadius, separationForce);

            // 4. Combine and clamp to max speed.
            Vector2 desiredVelocity = (seek + separation);
            if (desiredVelocity.sqrMagnitude > def.MoveSpeed * def.MoveSpeed)
            {
                desiredVelocity = desiredVelocity.normalized * def.MoveSpeed;
            }

            state.Velocity = desiredVelocity;
            state.Position += state.Velocity * deltaTime;

            // 5. Determine animation state.
            if (distToTarget <= def.AttackRange)
            {
                state.AnimState = 2; // Attack
            }
            else if (state.Velocity.sqrMagnitude > 0.01f)
            {
                state.AnimState = 1; // Walk
            }
            else
            {
                state.AnimState = 0; // Idle
            }

            return state;
        }

        private static Vector2 ComputeSeparation(
            EnemyState selfState,
            int selfIndex,
            NetworkArray<EnemyState> allEnemies,
            float radius,
            float force)
        {
            Vector2 separation = Vector2.zero;
            float   radiusSq   = radius * radius;
            int     count      = 0;

            // Iterate up to MaxEnemies; only check a neighbourhood window to keep
            // per-tick cost manageable when hundreds of enemies are active.
            int start = Mathf.Max(0, selfIndex - SeparationNeighbourhoodRadius);
            int end   = Mathf.Min(allEnemies.Length, selfIndex + SeparationNeighbourhoodRadius);

            for (int j = start; j < end; j++)
            {
                if (j == selfIndex) continue;

                var other = allEnemies[j];
                if (!other.IsActive) continue;

                Vector2 delta = selfState.Position - other.Position;
                float   sq    = delta.sqrMagnitude;
                if (sq < radiusSq && sq > 0.0001f)
                {
                    separation += delta.normalized * (force * (1f - Mathf.Sqrt(sq) / radius));
                    count++;
                }
            }

            return separation;
        }
    }
}
