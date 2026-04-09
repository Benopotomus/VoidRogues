// using DWD.Pooling; // TODO: Port from LichLord
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterHitReactComponent : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        [SerializeField]
        private List<HitReactionDefinition> _hitReacts = new List<HitReactionDefinition>();

        int _hitReactEndTick;

        [SerializeField]
        private Transform _impactAttachment;

        private int _currentAdditiveReactIndex = 0;
        public int CurrentAdditiveReactIndex => _currentAdditiveReactIndex;

        int _additiveHitReactEndTick;

        [SerializeField]
        private List<AdditiveHitReactionDefinition> _additiveHitReacts = new List<AdditiveHitReactionDefinition>();

        // TODO: Port VisualEffectSpawner from LichLord
        // private VisualEffectSpawner _visualSpawner = new VisualEffectSpawner();

        public void UpdateHitReactState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            if (tick > _hitReactEndTick)
            {
                runtimeState.SetState(ENPCState.Idle);
            }
        }

        public void UpdateAdditiveHitReactState(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {
            int hitReactIndex = runtimeState.GetAdditiveHitReact();

            if (hitReactIndex > 0 &&
                tick > _additiveHitReactEndTick)
            {
                runtimeState.SetAdditiveHitReact(0);
            }

            if (hitReactIndex > 0 &&
                _currentAdditiveReactIndex != hitReactIndex)
            {
                StartAdditiveHitReact(hitReactIndex, tick);
                _currentAdditiveReactIndex = hitReactIndex;
            }
        }

        public void StartHitReact(ENPCState state, int animIndex, int tick)
        {
            if (animIndex > _hitReacts.Count)
                return;

            HitReactionDefinition hitReact = _hitReacts[animIndex];
            var animTrigger = hitReact.AnimationTrigger;

            _hitReactEndTick = tick + hitReact.TickDuration;
            _npc.AnimationController.SetAnimationForTrigger(animTrigger);

            // TODO: Port visual effect spawning from LichLord
            // SpawnImpactVisualEffect(animIndex);
        }

        public void StartAdditiveHitReact(int reactIndex, int tick)
        {
            if (_additiveHitReacts.Count == 0)
                return;

            AdditiveHitReactionDefinition additiveHitReact = _additiveHitReacts[reactIndex];
            var animTrigger = additiveHitReact.AdditiveAnimationTrigger;

            _npc.AnimationController.SetAdditiveAnimationForTrigger(animTrigger);

            // TODO: Port visual effect spawning from LichLord
            // SpawnImpactVisualEffect(reactIndex);

            _additiveHitReactEndTick = tick + 16;
        }
    }
}
