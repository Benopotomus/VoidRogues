using System;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterLifetimeComponent : MonoBehaviour
    {
        [SerializeField]
        private int _nextLifetimeProgressTick;

        [SerializeField]
        private int _lifetimeProgress;
        public int LifetimeProgress => _lifetimeProgress;

        [SerializeField]
        private int _lifetimeProgressMax;
        public int LifetimeProgressMax => _lifetimeProgressMax;

        public Action<int, int> OnLifetimeProgressChanged;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState, int tick)
        {

        }

        public void UpdateLifetime(NonPlayerCharacterRuntimeState runtimeState,
            bool hasAuthority,
            int tick)
        {
            
        }
    }
}
