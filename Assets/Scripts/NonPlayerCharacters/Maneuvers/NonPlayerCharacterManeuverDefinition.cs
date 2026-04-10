// using LichLord.World; // TODO: Port from LichLord
// using Rukhanka; // TODO: Port from LichLord
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "VoidRogues/Maneuvers/NPCManeuverDefinition", order = 1)]
    public class NonPlayerCharacterManeuverDefinition : ScriptableObject
    {
        [SerializeField]
        protected string ActionName;

        public virtual EManeuverType ManeuverType => EManeuverType.None;

        [SerializeField]
        private int _cooldownTicks = 32;
        public int CooldownTicks => _cooldownTicks;

        [SerializeField]
        private Vector2 _activationRange = new Vector2(0, 2.5f);
        public Vector2 ActivationRange => _activationRange;

        [SerializeField]
        private float _attackRange = 3f;
        public float AttackRange => _attackRange;

        [SerializeField]
        private int _stateTicks = 32;
        public int StateTicks => _stateTicks;

        [SerializeField]
        private float _faceTargetRange = 5f;

        [SerializeField]
        private bool _requiresLOS = true;
        public bool RequiresLOS => _requiresLOS;

        public float FaceTargetRangeSqrt => _faceTargetRange * _faceTargetRange;

        public bool IsInActivationRange(float sqrDist)
        {
            return (sqrDist > (_activationRange.x * _activationRange.x) &&
               sqrDist < (_activationRange.y * _activationRange.y));
        }

        [SerializeField]
        private float _verticalAimOffset;
        public float VerticalAimOffset => _verticalAimOffset;

        [Header("Targeting")]
        [SerializeField]
        private Vector2 _validTargetDistance = Vector2.zero;
        public Vector2 ValidTargetDistance => _validTargetDistance;

        [Header("Animations")]
        [SerializeField]
        private List<FAnimationTrigger> _animationTriggers = new List<FAnimationTrigger>();
        public List<FAnimationTrigger> AnimationTriggers => _animationTriggers;

        [Header("Events")]
        [SerializeField]
        private List<NonPlayerCharacterManeuverHitEvent> _hitEvents = new List<NonPlayerCharacterManeuverHitEvent>();
        public List<NonPlayerCharacterManeuverHitEvent> HitEvents => _hitEvents;

        [SerializeField]
        private List<NonPlayerCharacterManeuverHitEvent> _specialEvents = new List<NonPlayerCharacterManeuverHitEvent>();
        public List<NonPlayerCharacterManeuverHitEvent> SpecialEvents => _specialEvents;

        public virtual bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            return false;
        }

        // TODO: Port IChunkTrackable from LichLord
        // public void ExecuteHitEvents(NonPlayerCharacter npc, IChunkTrackable target)
        // {
        //     foreach(var hitEvent in HitEvents)
        //         hitEvent.Execute(npc, this, target);
        // }

        // public void ExecuteSpecialEvents(NonPlayerCharacter npc, IChunkTrackable target)
        // {
        //     foreach (var specialEvent in SpecialEvents)
        //         specialEvent.Execute(npc, this, target);
        // }

        // public Vector3 GetMovementToActivationRange(NonPlayerCharacter npc, IChunkTrackable target)
        // {
        //     Vector3 predictedPos = target.Position;
        //     Vector3 directionToTarget = (npc.Position - predictedPos).normalized;
        //     return target.Position + (directionToTarget * ((_activationRange.x + _activationRange.y) * 0.5f));
        // }
    }
}
