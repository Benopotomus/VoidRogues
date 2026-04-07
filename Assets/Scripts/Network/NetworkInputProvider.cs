using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidRogues.Network
{
    /// <summary>
    /// Polls the Unity Input System each render frame and packages the values into
    /// <see cref="NetworkInputData"/> for Photon Fusion.
    ///
    /// Follows the Hallowheart / LichLord <c>PlayerCharacterInput</c> pattern:
    /// <list type="bullet">
    ///   <item>One-frame actions (pressed this frame) are accumulated with |= in
    ///         <see cref="Update"/> so no press is missed between Fusion ticks.</item>
    ///   <item>Held actions use IsPressed() and are set every render frame.</item>
    ///   <item><see cref="ResetInput"/> zeroes all one-frame fields after each tick
    ///         so they do not bleed into the next tick.</item>
    /// </list>
    ///
    /// Attach to the same GameObject as the <see cref="NetworkRunner"/> and register
    /// via <c>runner.AddCallbacks(this)</c> (done by <see cref="NetworkBootstrap"/>
    /// or <see cref="MissionLauncher"/>).
    /// </summary>
    public class NetworkInputProvider : MonoBehaviour, INetworkRunnerCallbacks
    {
        // ── Cached input state ───────────────────────────────────────────────
        private Vector2 _aimWorldPos;

        // Fire – accumulated one-frame / current held
        private bool _fire;
        private bool _fireHeld;

        // Interact – accumulated one-frame / current held
        private bool _interact;
        private bool _interactHeld;

        // ── Input actions ────────────────────────────────────────────────────
        private VoidRoguesInputActions _actions;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            _actions = new VoidRoguesInputActions();
            _actions.Enable();
        }

        private void OnDestroy()
        {
            _actions.Disable();
            _actions.Dispose();
        }

        /// <summary>
        /// Poll input every render frame, accumulating one-frame presses with |=
        /// so that no press is lost between Fusion simulation ticks.
        /// </summary>
        private void Update()
        {
            // Aim: convert mouse screen position to 2-D world position.
            if (Camera.main != null)
            {
                _aimWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            }

            // Fire: accumulate one-frame press; refresh held state every frame.
            _fire     |= _actions.Gameplay.Fire.WasPressedThisFrame();
            _fireHeld  = _actions.Gameplay.Fire.IsPressed();

            // Interact: same pattern.
            _interact     |= _actions.Gameplay.Interact.WasPressedThisFrame();
            _interactHeld  = _actions.Gameplay.Interact.IsPressed();
        }

        // ── INetworkRunnerCallbacks – input ──────────────────────────────────

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData
            {
                Move        = _actions.Gameplay.Move.ReadValue<Vector2>(),
                AimWorldPos = _aimWorldPos,
                Fire        = _fire,
                FireHeld    = _fireHeld,
                Interact    = _interact,
                InteractHeld = _interactHeld,
            };

            input.Set(data);

            // Reset one-frame inputs so they are not re-sent next tick.
            ResetInput();
        }

        /// <summary>
        /// Clears one-frame input fields.  Held fields are refreshed by
        /// <see cref="Update"/> each render frame and do not need resetting here.
        /// </summary>
        public void ResetInput()
        {
            _fire     = false;
            _interact = false;
        }

        // ── Unused INetworkRunnerCallbacks – required by interface ────────────

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    }
}
