// using DWD.Utility.Loading; // TODO: Port from LichLord
using System;
using UnityEngine;

namespace VoidRogues
{
    [Serializable]
    public class NonPlayerCharacterHitReactState
    {
        public float StateTime = 1f;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        // [BundleObject(typeof(GameObject))] // TODO: Port BundleObject from LichLord
        // [SerializeField]
        // private BundleObject _hitEffect;
        // public BundleObject HitEffect => _hitEffect;
    }
}
