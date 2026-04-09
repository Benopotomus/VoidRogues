using Cinemachine;
using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Top-down follow camera for the gameplay scene.
    /// Follows the observed player character via Cinemachine.
    /// </summary>
    public partial class SceneCamera : SceneService
    {
        [SerializeField]
        public Transform _cameraFollowTarget;

        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera topDownCam;

        protected override void OnInitialize()
        {
            Camera camera = Context.Runner.SimulationUnityScene.FindMainCamera();
            if (camera != null)
            {
                camera.gameObject.SetActive(true);
            }

            if (topDownCam != null)
            {
                topDownCam.Follow = _cameraFollowTarget;
                topDownCam.LookAt = _cameraFollowTarget;
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            if (Camera.main == null)
                return;

            // Follow the observed player character
            PlayerCharacter observed = Context.ObservedPlayerCharacter;
            if (observed != null)
            {
                _cameraFollowTarget.position = observed.transform.position;
            }
        }
    }
}
