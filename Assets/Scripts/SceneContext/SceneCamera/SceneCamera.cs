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

        /// <summary>
        /// The transform the camera is actively following.
        /// Set explicitly by the player character on spawn via <see cref="SetCameraFollow"/>.
        /// </summary>
        private Transform _followTransform;

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

            // Tilt the follow target downward so the camera looks down at 60 degrees
            _cameraFollowTarget.rotation = Quaternion.Euler(60f, 0f, 0f);
        }

        /// <summary>
        /// Explicitly sets the transform the camera should follow.
        /// Called by the local player character on spawn.
        /// </summary>
        public void SetCameraFollow(Transform target)
        {
            _followTransform = target;
        }

        protected override void OnLateTick()
        {
            base.OnLateTick();

            if (Camera.main == null)
                return;

            // Primary: follow the explicitly-set transform (set on spawn).
            // Unity's overridden == operator makes this null when the object is destroyed.
            if (_followTransform != null)
            {
                _cameraFollowTarget.position = _followTransform.position;
                return;
            }

            // Fallback: follow the observed player character from context
            PlayerCharacter observed = Context.ObservedPlayerCharacter;
            if (observed != null)
            {
                _cameraFollowTarget.position = observed.transform.position;
            }
        }
    }
}
