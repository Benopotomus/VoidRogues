using Fusion;
using UnityEngine;

namespace VoidRogues.Network
{
    /// <summary>
    /// Input struct sent from client to host every simulation tick.
    /// Must remain blittable – no managed references.
    ///
    /// Follows the Hallowheart / LichLord pattern: every button has a one-frame
    /// pressed field (set on the frame the button is first pressed) and a held
    /// field (true while the button is held down).  The one-frame fields are
    /// accumulated across render frames with |= and reset by the input provider
    /// after each Fusion tick via <see cref="NetworkInputProvider.ResetInput"/>.
    /// </summary>
    public struct NetworkInputData : INetworkInput
    {
        /// <summary>Normalised WASD / left-stick movement direction.</summary>
        public Vector2 Move;

        /// <summary>
        /// Mouse world position used by <see cref="VoidRogues.Player.PlayerShooter"/>
        /// to derive the aim direction (mouse world pos minus player pos).
        /// </summary>
        public Vector2 AimWorldPos;

        // ── Fire ────────────────────────────────────────────────────────────
        /// <summary>True on the frame the Fire button was first pressed.</summary>
        public bool Fire;
        /// <summary>True every frame while the Fire button is held.</summary>
        public bool FireHeld;

        // ── Interact ────────────────────────────────────────────────────────
        /// <summary>True on the frame the Interact button was first pressed.</summary>
        public bool Interact;
        /// <summary>True every frame while the Interact button is held.</summary>
        public bool InteractHeld;
    }
}
