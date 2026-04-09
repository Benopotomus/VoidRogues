using System;
using UnityEngine;

namespace VoidRogues
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
            if (!runtimeState.IsCommandedUnit())
                return;

            _lifetimeProgress = runtimeState.GetLifetimeProgress();
            _lifetimeProgressMax = runtimeState.GetLifetimeProgressMax();
            _nextLifetimeProgressTick = tick + runtimeState.GetTicksPerLifetime();
        }

        public void UpdateLifetime(NonPlayerCharacterRuntimeState runtimeState,
            bool hasAuthority,
            int tick)
        {
            if (!runtimeState.IsCommandedUnit())
                return;

            if (tick > _nextLifetimeProgressTick)
            {
                _lifetimeProgress = runtimeState.GetLifetimeProgress();
                int newlifetime = _lifetimeProgress + 1;

                runtimeState.SetLifetimeProgress(newlifetime);
                _nextLifetimeProgressTick = tick + runtimeState.GetTicksPerLifetime();

                if (newlifetime >= runtimeState.GetLifetimeProgressMax())
                {
                    runtimeState.SetState(ENPCState.Dead);
                }

                OnLifetimeProgressChanged?.Invoke(_lifetimeProgress, _lifetimeProgressMax);
            }
        }
    }
}
