// using DWD.Utility.Loading; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "AdditiveHitReactionDefinition", menuName = "VoidRogues/HitReactions/AdditiveHitReactionDefinition")]
    public class AdditiveHitReactionDefinition : ScriptableObject
    {
        [SerializeField]
        private FAdditiveAnimationTrigger _additiveAnimationTrigger;
        public FAdditiveAnimationTrigger AdditiveAnimationTrigger => _additiveAnimationTrigger;

        // [BundleObject(typeof(GameObject))] // TODO: Port BundleObject from LichLord
        // [SerializeField]
        // private BundleObject _hitEffect;
        // public BundleObject HitEffect => _hitEffect;
    }
}
