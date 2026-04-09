namespace VoidRogues
{
    using Fusion;
    using UnityEngine;

    /// <summary>
    /// Network input struct polled by Fusion each tick.
    /// Sent from the input-authority client to the server/host.
    /// </summary>
    public struct GameInput : INetworkInput
    {
        /// <summary>Raw 2D stick / WASD vector (not normalized).</summary>
        public Vector2 MoveDirection;

        /// <summary>Aim / look direction (screen-space pointer or right-stick).</summary>
        public Vector2 LookDirection;

        /// <summary>Packed button states.</summary>
        public NetworkButtons Buttons;
    }

    /// <summary>
    /// Button indices used with <see cref="NetworkButtons"/> inside <see cref="GameInput"/>.
    /// Keep in sync with InputManager eInputAction where applicable.
    /// </summary>
    public enum EInputButton
    {
        Attack     = 0,
        Special    = 1,
        Dodge      = 2,
        Interact   = 3,
        SkillZero  = 4,
        SkillOne   = 5,
        SwapWeapon = 6,
        Jump       = 7,
    }
}
