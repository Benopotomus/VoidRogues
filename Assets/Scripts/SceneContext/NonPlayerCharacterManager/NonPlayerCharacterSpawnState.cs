// using DWD.Utility.Loading; // TODO: Port from LichLord
using System;
using UnityEngine;

namespace VoidRogues
{
    [Serializable]
    public class NonPlayerCharacterSpawnState
    {
        public float StateTime = 2f;

        [SerializeField]
        private FAnimationTrigger _animationTrigger;
        public FAnimationTrigger AnimationTrigger => _animationTrigger;

        // [BundleObject(typeof(GameObject))] // TODO: Port BundleObject from LichLord
        // [SerializeField]
        // private BundleObject _spawnEffect;
        // public BundleObject SpawnEffect => _spawnEffect;
    }
}
