using System.Collections;
// using Unity.Entities; // TODO: Port ECS from LichLord
// using Unity.Transforms; // TODO: Port ECS from LichLord
using UnityEngine;
// using Rukhanka; // TODO: Port Rukhanka from LichLord
// using AYellowpaper.SerializedCollections; // TODO: Port from LichLord
// using LichLord.Projectiles; // TODO: Port from LichLord
using System.Collections.Generic;

namespace VoidRogues.NonPlayerCharacters
{
    public partial class NonPlayerCharacterAnimationController : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        // TODO: Port animation callback data with ProjectileDefinition from LichLord
        // [SerializeField]
        // [SerializedDictionary]
        // private SerializedDictionary<ProjectileDefinition, FAnimationCallbackData> _animationCallbacks;

        // TODO: Port ECS visual entity system from LichLord
        // private Entity visualEntity;
        // private EntityManager entityManager;

        // TODO: Port Rukhanka FastAnimatorParameter system from LichLord
        // private static readonly FastAnimatorParameter Moving = new("Moving");
        // ... (all other FastAnimatorParameter fields)

        [Header("Animation Smoothing")]
        private float velocitySmoothTime = 0.1f;
        private Vector3 smoothedLocalVelocity;
        private float smoothedYawVelocity;

        private float modelScale;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            const int TotalScaleSteps = 10;
            int scaleIndex = runtimeState.FullIndex % TotalScaleSteps;
            modelScale = Mathf.Lerp(runtimeState.Definition.ModelScale.x,
                runtimeState.Definition.ModelScale.y,
                scaleIndex / (TotalScaleSteps - 1f));

            // TODO: Port ECS visual entity spawning from LichLord
        }

        public void SetAnimationForTrigger(FAnimationTrigger animationTrigger, bool forceWeaponId = false)
        {
            // TODO: Port Rukhanka animation parameter setting from LichLord
        }

        public void SetAdditiveAnimationForTrigger(FAdditiveAnimationTrigger additiveAnimationTrigger)
        {
            // TODO: Port Rukhanka animation parameter setting from LichLord
        }

        public void SetAnimationForState(ENPCState oldState, ENPCState newState)
        {
            // TODO: Port Rukhanka animation state transitions from LichLord
        }

        public void UpdateAnimatonForMovement(NonPlayerCharacterRuntimeState runtimeState, Vector3 localVelocity, float yawVelocity, float renderDeltaTime)
        {
            if (runtimeState.GetState() != ENPCState.Idle)
                return;

            float walkSpeed = runtimeState.Definition.WalkSpeed;

            float smoothRate = UnityEngine.Time.deltaTime / velocitySmoothTime;
            smoothedLocalVelocity = Vector3.Lerp(smoothedLocalVelocity, localVelocity / walkSpeed, smoothRate);
            smoothedYawVelocity = Mathf.Lerp(smoothedYawVelocity, yawVelocity, smoothRate);

            // TODO: Port Rukhanka animation parameter updates from LichLord
        }

        public void SyncTransformToEntity()
        {
            // TODO: Port ECS transform sync from LichLord
        }

        public void UpdateAnimationEvents()
        {
            // TODO: Port ECS animation event processing from LichLord
        }
    }
}
