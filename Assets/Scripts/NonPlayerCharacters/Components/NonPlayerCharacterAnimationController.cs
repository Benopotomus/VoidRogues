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
        private NonPlayerCharacter _npc;

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

        public void OnSpawned(FNonPlayerCharacterData data)
        {
            _npc = GetComponent<NonPlayerCharacter>();



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

        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
            float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick)
        { 
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
