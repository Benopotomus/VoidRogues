using Fusion;
using UnityEngine;

namespace VoidRogues.Network
{
    /// <summary>
    /// Input struct sent from client to host every simulation tick.
    /// Must remain blittable – no managed references.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        /// <summary>Normalised WASD / left-stick movement direction.</summary>
        public Vector2 Move;

        /// <summary>
        /// Mouse world position (used by <see cref="VoidRogues.Player.PlayerShooter"/>
        /// to derive aim direction by subtracting player position).
        /// </summary>
        public Vector2 AimWorldPos;

        /// <summary>Bitfield for button states (Fire, Interact, Reload …).</summary>
        public NetworkButtons Buttons;
    }

    /// <summary>Bit indices for <see cref="NetworkInputData.Buttons"/>.</summary>
    public enum InputButton
    {
        Fire     = 0,
        Interact = 1,
        Reload   = 2,
    }
}
