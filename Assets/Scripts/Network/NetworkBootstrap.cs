using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRogues.Network
{
    /// <summary>
    /// Placed on a single GameObject in the Bootstrap scene (Build Index 0).
    /// Starts the Photon Fusion 2 session and transitions to the Ship scene.
    /// </summary>
    public class NetworkBootstrap : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Prefabs")]
        [SerializeField] private NetworkRunner _runnerPrefab;
        [SerializeField] private NetworkObject _playerPrefab;

        [Header("Session")]
        [SerializeField] private string _defaultSessionName = "VoidRogues";
        [SerializeField] private int    _maxPlayers         = 4;

        [Header("Editor Quick-Start")]
        [Tooltip("When true the editor auto-start skips the Ship hub and loads the " +
                 "Mission scene (Build Index 2) directly, matching the MissionLauncher flow.")]
        [SerializeField] private bool _skipToMission = false;

        private NetworkRunner _runner;

        private void Start()
        {
#if UNITY_EDITOR
            // In the Editor, auto-start as Host.
            // Set _skipToMission = true to bypass the Ship hub and go straight to
            // Mission (Build Index 2) – mirrors the MissionLauncher direct-launch flow.
            int targetScene = _skipToMission ? 2 : 1;
            StartSession(GameMode.Host, _defaultSessionName, targetScene).Forget();
#endif
        }

        // ------------------------------------------------------------------
        // Public API (called from the connect menu in standalone builds)
        // ------------------------------------------------------------------

        public void StartHost(string sessionName) =>
            StartSession(GameMode.Host, sessionName, 1).Forget();

        public void StartClient(string sessionName) =>
            StartSession(GameMode.Client, sessionName, 1).Forget();

        // ------------------------------------------------------------------
        // Session startup
        // ------------------------------------------------------------------

        private async Task StartSession(GameMode mode, string sessionName, int targetSceneIndex)
        {
            _runner = Instantiate(_runnerPrefab);
            _runner.AddCallbacks(this);
            _runner.ProvideInput = true;

            var scene = SceneRef.FromIndex(targetSceneIndex);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode    = mode,
                SessionName = sessionName,
                PlayerCount = _maxPlayers,
                Scene       = scene,
                SceneManager = _runner.GetComponent<NetworkSceneManagerDefault>(),
            });

            if (!result.Ok)
            {
                Debug.LogError($"[NetworkBootstrap] Failed to start session: {result.ShutdownReason}");
            }
        }

        // ------------------------------------------------------------------
        // INetworkRunnerCallbacks – player lifecycle
        // ------------------------------------------------------------------

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;

            var spawnPos = GetSpawnPosition(player);
            runner.Spawn(_playerPrefab, spawnPos, Quaternion.identity, player);
        }

        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;

            if (runner.TryGetPlayerObject(player, out var playerObject))
            {
                runner.Despawn(playerObject);
            }
        }

        // ------------------------------------------------------------------
        // INetworkRunnerCallbacks – input
        // ------------------------------------------------------------------

        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            // Input polling is handled by the local NetworkInputProvider.
            // See NetworkInputProvider.cs.
        }

        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }

        // ------------------------------------------------------------------
        // INetworkRunnerCallbacks – connection/shutdown
        // ------------------------------------------------------------------

        public void OnConnectedToServer(NetworkRunner runner)    => Debug.Log("[NetworkBootstrap] Connected to server.");
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
            => Debug.LogWarning($"[NetworkBootstrap] Disconnected: {reason}");
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
            => Debug.LogError($"[NetworkBootstrap] Connect failed: {reason}");
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessage message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, System.ArraySegment<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, float progress) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
            => Debug.Log($"[NetworkBootstrap] Session shut down: {shutdownReason}");

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private Vector2 GetSpawnPosition(PlayerRef player)
        {
            // Spread players apart at spawn so they don't overlap.
            float offset = (player.AsIndex % _maxPlayers) * 1.5f;
            return new Vector2(offset - (_maxPlayers * 0.75f), 0f);
        }
    }
}
