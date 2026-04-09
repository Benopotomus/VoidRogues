namespace VoidRogues
{
    using Fusion.Addons.KCC;
    using UnityEngine;

    /// <summary>
    /// KCC processor that disables gravity and restricts movement to the XZ plane.
    /// The character will maintain its current Y position at all times.
    /// Add this as a local processor on the KCC component (via Settings > Processors).
    /// </summary>
    public class NoGravityXZMovementProcessor : KCCProcessor, ISetGravity, ISetDynamicVelocity, ISetKinematicDirection, IAfterMoveStep
    {
        // Run after the default EnvironmentProcessor (priority 1000)
        public override float GetPriority(KCC kcc) => 1500;

        // ISetGravity – zero out gravity entirely
        public void Execute(ISetGravity stage, KCC kcc, KCCData data)
        {
            data.Gravity = Vector3.zero;
            kcc.SuppressProcessors<NoGravityXZMovementProcessor>();
        }

        // ISetDynamicVelocity – strip any vertical dynamic velocity (gravity residue, external forces, etc.)
        public void Execute(ISetDynamicVelocity stage, KCC kcc, KCCData data)
        {
            Vector3 dynamicVelocity = data.DynamicVelocity;
            dynamicVelocity.y = 0f;
            data.DynamicVelocity = dynamicVelocity;
        }

        // ISetKinematicDirection – ensure input never produces vertical movement
        public void Execute(ISetKinematicDirection stage, KCC kcc, KCCData data)
        {
            Vector3 dir = data.KinematicDirection;
            dir.y = 0f;
            data.KinematicDirection = dir;
        }

        // IAfterMoveStep – final safety clamp: kill any residual Y velocity after each move step
        public void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            Vector3 kinematic = data.KinematicVelocity;
            kinematic.y = 0f;
            data.KinematicVelocity = kinematic;

            Vector3 dynamic = data.DynamicVelocity;
            dynamic.y = 0f;
            data.DynamicVelocity = dynamic;
        }
    }
}