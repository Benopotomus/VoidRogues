using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VoidRogues.Network
{
    /// <summary>
    /// Polls the Unity Input System each tick and packages values into
    /// <see cref="NetworkInputData"/> for Photon Fusion.
    ///
    /// Attach to the same GameObject as <see cref="NetworkBootstrap"/> and
    /// register this as an INetworkRunnerCallbacks listener (done automatically
    /// via runner.AddCallbacks in NetworkBootstrap).
    /// </summary>
    public class NetworkInputProvider : MonoBehaviour, INetworkRunnerCallbacks
    {
        // Aim position is calculated from mouse world position in Render;
        // it is cached here so OnInput (called mid-tick) can read it.
        private Vector2 _aimDirection;
        private bool    _firePressed;
        private bool    _interactPressed;

        private VoidRoguesInputActions _actions;

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

        private void Update()
        {
            // Cache aim direction from mouse position (world space).
            // Camera.main is acceptable here since this runs once per render frame.
            if (Camera.main != null)
            {
                var mouseWorld = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
                _aimDirection = mouseWorld;   // absolute world pos; PlayerShooter subtracts player pos
            }

            _firePressed    = _actions.Gameplay.Fire.IsPressed();
            _interactPressed = _actions.Gameplay.Interact.IsPressed();
        }

        // ------------------------------------------------------------------
        // INetworkRunnerCallbacks – input
        // ------------------------------------------------------------------

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            var data = new NetworkInputData
            {
                Move          = _actions.Gameplay.Move.ReadValue<Vector2>(),
                AimWorldPos   = _aimDirection,
            };

            data.Buttons.Set(InputButton.Fire,     _firePressed);
            data.Buttons.Set(InputButton.Interact, _interactPressed);

            input.Set(data);
        }

        // ------------------------------------------------------------------
        // Unused INetworkRunnerCallbacks – required by interface
        // ------------------------------------------------------------------

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessage message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, float progress) { }
    }
}
