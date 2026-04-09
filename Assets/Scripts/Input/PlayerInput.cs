namespace VoidRogues
{
    using Fusion;
    using Fusion.Sockets;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Reads local input from <see cref="InputManager"/> every Unity frame and
    /// feeds a <see cref="GameInput"/> struct into Fusion's input pipeline via
    /// <see cref="INetworkRunnerCallbacks.OnInput"/>.
    /// 
    /// Attach this component to any persistent GameObject in the gameplay scene
    /// (e.g. the same object as NetworkGame) and call <see cref="SetRunner"/>
    /// once the <see cref="NetworkRunner"/> is available.
    /// </summary>
    [DefaultExecutionOrder(-15)] // After InputManager (-20)
    public sealed class PlayerInput : MonoBehaviour, INetworkRunnerCallbacks
    {
        // SINGLETON
        public static PlayerInput Instance { get; private set; }

        // PRIVATE STATE
        private NetworkRunner _runner;
        private GameInput _accumulatedInput;

        // LIFECYCLE

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            DetachRunner();

            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_runner == null)
                return;

            var manager = InputManager.instance;
            if (manager == null)
                return;

            // Movement – always overwrite with the latest frame value
            _accumulatedInput.MoveDirection = manager.inputVector;
            _accumulatedInput.LookDirection = manager.pointerWorldPosition;

            // Buttons – OR-accumulate so short presses between ticks are never lost
            var buttons = _accumulatedInput.Buttons;

            buttons.Set(EInputButton.Attack,     manager.PlayerInputs.TryGetValue(eInputAction.Attack,     out var atk) && atk.isDown);
            buttons.Set(EInputButton.Special,    manager.PlayerInputs.TryGetValue(eInputAction.Special,    out var spc) && spc.isDown);
            buttons.Set(EInputButton.Dodge,      manager.PlayerInputs.TryGetValue(eInputAction.Dodge,      out var ddg) && ddg.isDown);
            buttons.Set(EInputButton.Interact,   manager.PlayerInputs.TryGetValue(eInputAction.Interact,   out var itr) && itr.isDown);
            buttons.Set(EInputButton.SkillZero,  manager.PlayerInputs.TryGetValue(eInputAction.SkillZero,  out var sk0) && sk0.isDown);
            buttons.Set(EInputButton.SkillOne,   manager.PlayerInputs.TryGetValue(eInputAction.SkillOne,   out var sk1) && sk1.isDown);
            buttons.Set(EInputButton.SwapWeapon, manager.PlayerInputs.TryGetValue(eInputAction.SwapWeapon, out var swp) && swp.isDown);

            _accumulatedInput.Buttons = buttons;
        }

        // PUBLIC API

        /// <summary>
        /// Register this provider with a <see cref="NetworkRunner"/>.
        /// Call once after the runner starts (e.g. in NetworkGame.Initialize).
        /// </summary>
        public void SetRunner(NetworkRunner runner)
        {
            DetachRunner();

            if (runner == null)
                return;

            _runner = runner;
            _runner.AddCallbacks(this);
        }

        /// <summary>
        /// Unregister from the current runner.
        /// </summary>
        public void DetachRunner()
        {
            if (_runner != null)
            {
                _runner.RemoveCallbacks(this);
                _runner = null;
            }
        }

        // INetworkRunnerCallbacks – OnInput

        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            input.Set(_accumulatedInput);

            // Reset for next accumulation window
            _accumulatedInput = default;
        }

        // INetworkRunnerCallbacks – unused stubs

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner) { }
        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner) { }
    }
}
