using UnityEngine;
using Fusion;

namespace VoidRogues
{
    public class TargetFramerate : MonoBehaviour
    {
        [SerializeField]
        private bool _useFusionTime = true;
        [SerializeField]
        private int _targetFrameRate = 90;

        private void Awake()
        {
            int target = _targetFrameRate;
            if (_useFusionTime)
            {
                target = TickRate.Resolve(NetworkProjectConfig.Global.Simulation.TickRateSelection).Server;
                Application.targetFrameRate = target;
            }
            else
            {
                Application.targetFrameRate = _targetFrameRate;
            }
            Debug.Log("Setting Target Framerate to: " +  target);
        }
    }
}