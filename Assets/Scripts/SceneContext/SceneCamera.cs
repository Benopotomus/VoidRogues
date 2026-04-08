using Cinemachine;
using System;
using UnityEngine;

namespace VoidRogues
{
    public partial class SceneCamera : SceneService
    {
        [SerializeField] 
        private Transform _skydomeTransform;

        [SerializeField]
        public Transform _cameraFollowTarget;

        public Transform followTransform;

        [Header("Cinemachine Cameras")]
        [SerializeField] private CinemachineVirtualCamera thirdPersonCam;
        [SerializeField] private CinemachineVirtualCamera firstPersonCam;

        [Header("Raycast Settings")]
        [SerializeField] private float _minRaycastDistance = 2.7f;
        [SerializeField] private float _maxRaycastDistance = 100f;
        [SerializeField] private LayerMask _raycastLayerMask;

        private Vector3 _reticlePosition = Vector3.zero;
        public Vector3 ReticlePosition => _reticlePosition;

        [SerializeField] private Vector2 _rayOrigin = new Vector2(0.5f, 0.5f);

        public Action<Vector3> OnReticlePositionChanged;

        private float sphereRadius = 0.1f; // Radius of the debug sphere

        private bool isFirstPerson = false;

        [SerializeField]
        private FCachedRaycast _cachedRaycastHit; // Store last hit/max range point
        public FCachedRaycast CachedRaycastHit => _cachedRaycastHit;

        private bool lastRaycastHit; // True if last raycast hit something

        protected override void OnInitialize()
        {
            Camera camera = Context.Runner.SimulationUnityScene.FindMainCamera();
            if (camera != null)
            {
                camera.gameObject.SetActive(true);
            }

            // Set initial camera view
            SetCameraView(isFirstPerson);

            thirdPersonCam.Follow = _cameraFollowTarget;
            thirdPersonCam.LookAt = _cameraFollowTarget;
            firstPersonCam.Follow = _cameraFollowTarget;
            firstPersonCam.LookAt = _cameraFollowTarget;
        }

        public void ModifyCameraTargetRotation(Quaternion newRotation)
        {
            _cameraFollowTarget.rotation = newRotation;
        }

        public void SetCameraFollow(Transform transform)
        {
            followTransform = transform;
        }

        public void SetCameraView(bool firstPerson)
        {
            isFirstPerson = firstPerson;

            if (firstPersonCam != null && thirdPersonCam != null)
            {
                firstPersonCam.Priority = firstPerson ? 20 : 10;
                thirdPersonCam.Priority = firstPerson ? 10 : 20;

                Debug.Log($"[CameraManager] Switched to {(firstPerson ? "First Person" : "Third Person")} view");
            }
            else
            {
                Debug.LogError("[CameraManager] One or both cameras not assigned!");
            }
        }

        protected override void OnTick()
        {
            base.OnTick();
            PlayerCharacter localPlayerCreature = Context.LocalPlayerCharacter;

            Camera mainCamera = Camera.main;

            if (mainCamera == null)
            {
                _cachedRaycastHit.Clear();
                return;
            }

            Transform cameraTransform = mainCamera.transform;
            Vector3 cameraPosition = mainCamera.transform.position;

            // Create a ray from the center of the camera viewport (slightly upward)
            Ray ray = mainCamera.ViewportPointToRay(new Vector3(_rayOrigin.x, _rayOrigin.y, 0));

            // Offset the ray origin forward in third person mode
            float minDistance = isFirstPerson ? 0f : _minRaycastDistance;
            Vector3 rayOrigin = ray.origin + ray.direction * minDistance;
            Vector3 rayDirection = ray.direction;

            if (_skydomeTransform != null)
            {
                _skydomeTransform.position = cameraPosition;
            }

            if (localPlayerCreature == null)
                return;

            RaycastFromCameraCenter(rayOrigin, rayDirection, localPlayerCreature.gameObject);

            if (followTransform != null)
            {
                _cameraFollowTarget.position = followTransform.position;
            }

            // Update reticle position
            UpdateReticlePosition(rayOrigin, rayDirection);
        }

        public void RaycastFromCameraCenter(Vector3 rayOrigin, Vector3 rayDirection, GameObject ignoredObject)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] Main camera not found!");
                _cachedRaycastHit = new FCachedRaycast();
                return;
            }

            RaycastHit[] hits = Physics.RaycastAll(rayOrigin, rayDirection, _maxRaycastDistance, _raycastLayerMask, QueryTriggerInteraction.Collide);

            // Track the closest world (solid geometry) hit
            RaycastHit closestWorldHit = default;
            float closestWorldDistance = float.MaxValue;
            bool foundValidWorldHit = false;

            foreach (var hit in hits)
            {
                // Skip ignored object and its children (usually the player)
                if (ignoredObject != null &&
                    (hit.collider.gameObject == ignoredObject || hit.collider.transform.IsChildOf(ignoredObject.transform)))
                    continue;

                float dist = hit.distance;

                // World / solid geometry – only the CLOSEST one
                if (dist < closestWorldDistance)
                {
                    closestWorldDistance = dist;
                    closestWorldHit = hit;
                    foundValidWorldHit = true;
                }
            }

            // Final positions
            Vector3 maxRangePoint = rayOrigin + rayDirection * _maxRaycastDistance;

            // staticPosition = always the closest solid world geometry (or max range)
            if (foundValidWorldHit)
            {
                _cachedRaycastHit.staticPosition = closestWorldHit.point;
            }
            else
            {
                _cachedRaycastHit.staticPosition = maxRangePoint;
            }

            // position = closest world hit or max range
            if (foundValidWorldHit)
            {
                _cachedRaycastHit.position = closestWorldHit.point;
                _cachedRaycastHit.raycastHit = closestWorldHit;
                lastRaycastHit = true;
            }
            else
            {
                _cachedRaycastHit.position = maxRangePoint;
                _cachedRaycastHit.raycastHit = default;
                lastRaycastHit = false;
            }
        }

        private void UpdateReticlePosition(Vector3 rayOrigin, Vector3 rayDirection)
        {
            if (Camera.main == null)
                return;

            Camera mainCamera = Camera.main;

            // Project the raycast position to screen space
            Vector3 worldPosition = _cachedRaycastHit.position;
            Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

            // Clamp to viewport to keep reticle on screen
            Rect viewportRect = new Rect(0, 0, Screen.width, Screen.height);
            screenPosition.x = Mathf.Clamp(screenPosition.x, viewportRect.xMin, viewportRect.xMax);
            screenPosition.y = Mathf.Clamp(screenPosition.y, viewportRect.yMin, viewportRect.yMax);

            _reticlePosition = screenPosition;
            OnReticlePositionChanged?.Invoke(ReticlePosition);
        }

        private void OnDrawGizmos()
        {
            if (_cachedRaycastHit.position != Vector3.zero)
            {
                // Red = interaction point (what the reticle / player is "looking at")
                Gizmos.color = lastRaycastHit ? Color.red : Color.yellow; // yellow = no valid hit, at max range
                Gizmos.DrawWireSphere(_cachedRaycastHit.position, sphereRadius * 1.2f); // slightly larger for visibility
            }

            if (_cachedRaycastHit.staticPosition != Vector3.zero)
            {
                // Blue = closest solid world geometry stop (for building/placement)
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_cachedRaycastHit.staticPosition, sphereRadius);
            }
        }
    }

    [Serializable]
    public struct FCachedRaycast
    {
        public RaycastHit raycastHit;
        public Vector3 position;
        public Vector3 staticPosition;

        public void Clear()
        {
        }
    }
}
