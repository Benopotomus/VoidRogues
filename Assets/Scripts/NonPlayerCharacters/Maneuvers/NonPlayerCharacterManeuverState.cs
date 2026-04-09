// using LichLord.Projectiles; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    [Serializable]
    public class NonPlayerCharacterManeuverState
    {
        public NonPlayerCharacterManeuverDefinition Definition;
        public ENPCState ActiveState = ENPCState.Maneuver_1;
        public int CooldownExpirationTick;
        public int ActivationExpirationTick;
        public int ActivationTick;
        public float RandomAimOffset = 7f;
        public bool IsEnabled = true;

        public bool IsValid()
        {
            if (Definition == null)
                return false;

            return true;
        }

        public bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if (!IsEnabled)
                return false;

            if (Definition == null)
                return false;

            if (IsOnCooldown(tick))
                return false;

            bool canBeSelect = Definition.CanBeSelected(brainComponent, tick);

            return canBeSelect;
        }

        public bool IsOnCooldown(int tick)
        {
            return CooldownExpirationTick > tick;
        }

        public bool HasExpired(int tick)
        {
            return ActivationExpirationTick < tick;
        }

        public bool ExecuteManeuver(NonPlayerCharacter npc,
            NonPlayerCharacterRuntimeState runtimeState,
            int tick)
        {
            var oldState = runtimeState.GetState();

            if (oldState != ENPCState.Idle)
                return false;

            if (IsOnCooldown(tick))
                return false;

            // TODO: Port MeleeHitTracker from LichLord
            // npc.MeleeHitTracker.HitsPerSwing.Clear();

            runtimeState.SetState(ActiveState);

            int currentAnimIndex = runtimeState.GetAnimationIndex();
            int newAnimIndex = UnityEngine.Random.Range(0, Definition.AnimationTriggers.Count);

            if (newAnimIndex == currentAnimIndex)
            {
                newAnimIndex = (currentAnimIndex + 1) % Definition.AnimationTriggers.Count;
            }

            runtimeState.SetAnimationIndex(newAnimIndex);

            ActivationTick = tick;
            CooldownExpirationTick = ActivationTick + Definition.CooldownTicks;
            ActivationExpirationTick = ActivationTick + Definition.StateTicks;

            return true;
        }

        public void UpdateManeuverTick(NonPlayerCharacter npc, int tick)
        {
            // TODO: Port projectile spawning from LichLord (requires ProjectileManager, FManeuverProjectile)
        }
    }
}
