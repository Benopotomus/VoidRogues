using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidRogues.Player
{
    /// <summary>
    /// Reads Unity Input System actions and exposes them as typed events/properties
    /// consumed by other player components. Keeps input logic isolated from
    /// movement, combat, and UI code.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerInputReader : MonoBehaviour
    {
        /// <summary>Normalised movement direction this frame.</summary>
        public Vector2 MoveInput { get; private set; }

        /// <summary>World-space aim direction (mouse or right-stick).</summary>
        public Vector2 AimInput { get; private set; }

        /// <summary>True while the fire button is held.</summary>
        public bool IsFiring { get; private set; }

        /// <summary>Raised on dodge-roll button press.</summary>
        public event Action OnDodge;

        /// <summary>Raised on interact button press.</summary>
        public event Action OnInteract;

        /// <summary>Raised on pause button press.</summary>
        public event Action OnPause;

        private Camera _mainCamera;

        private void Awake()
        {
            _mainCamera = Camera.main;
        }

        // ── Input System callbacks (wired via PlayerInput component) ──────────

        public void OnMove(InputValue value)
        {
            MoveInput = value.Get<Vector2>();
        }

        public void OnAim(InputValue value)
        {
            Vector2 screenPos = value.Get<Vector2>();
            if (_mainCamera != null)
                AimInput = (Vector2)_mainCamera.ScreenToWorldPoint(screenPos);
        }

        public void OnFire(InputValue value)
        {
            IsFiring = value.isPressed;
        }

        public void OnDodgeRoll(InputValue value)
        {
            if (value.isPressed)
                OnDodge?.Invoke();
        }

        public void OnInteractButton(InputValue value)
        {
            if (value.isPressed)
                OnInteract?.Invoke();
        }

        public void OnPauseButton(InputValue value)
        {
            if (value.isPressed)
                OnPause?.Invoke();
        }
    }
}
