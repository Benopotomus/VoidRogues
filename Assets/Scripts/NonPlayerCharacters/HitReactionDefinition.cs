// using DWD.Utility.Loading; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "HitReactionDefinition", menuName = "VoidRogues/HitReactions/HitReactionDefinition")]
    public class HitReactionDefinition : ScriptableObject
    {
        [SerializeField]
        private bool _isAdditive;
        public bool IsAdditive => _isAdditive;

        [SerializeField]
        private int _tickDuration;
        public int TickDuration => _tickDuration;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        // [BundleObject(typeof(GameObject))] // TODO: Port BundleObject from LichLord
        // [SerializeField]
        // private BundleObject _hitEffect;
        // public BundleObject HitEffect => _hitEffect;
    }
}
