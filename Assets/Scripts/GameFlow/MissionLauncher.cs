using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using VoidRogues.Network;

namespace VoidRogues.GameFlow
{
    /// <summary>
    /// Placed in the Mission scene to launch a Fusion session directly into gameplay,
    /// following the Hallowheart / LichLord approach of loading straight into a game
    /// scene without requiring the Bootstrap → Ship flow.
    ///
    /// On Start it spins up a <see cref="NetworkRunner"/> on the same GameObject,
    /// adds a <see cref="NetworkInputProvider"/> for local input, and starts a
    /// Fusion session in the current scene.  The host immediately spawns every
    /// joining player via <see cref="OnPlayerJoined"/>.
    ///
    /// Inspector fields:
    /// <list type="bullet">
    ///   <item><see cref="_playerPrefab"/> – Fusion <see cref="NetworkObject"/> prefab
    ///         with <c>PlayerController</c> + <c>PlayerShooter</c>.</item>
    ///   <item><see cref="_gameMode"/> – <see cref="GameMode.Single"/> for offline
    ///         solo play; <see cref="GameMode.Host"/> for online multiplayer.</item>
    ///   <item><see cref="_sessionName"/> – Photon session name (Host/Client only).</item>
    ///   <item><see cref="_maxPlayers"/> – maximum player count for the session.</item>
    /// </list>
    /// </summary>
    public class MissionLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Player")]
        [SerializeField] private NetworkObject _playerPrefab;

        [Header("Session")]
        [SerializeField] private GameMode _gameMode   = GameMode.Single;
        [SerializeField] private string   _sessionName = "MissionDirect";
        [SerializeField] private int      _maxPlayers  = 4;

        private NetworkRunner _runner;

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Start()
        {
            LaunchGame().Forget();
        }

        // ── Session startup ──────────────────────────────────────────────────

        private async Task LaunchGame()
        {
            // Attach a NetworkRunner to this GameObject so it lives in the scene.
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;

            // Wire up the local input provider.
            var inputProvider = gameObject.AddComponent<NetworkInputProvider>();
            _runner.AddCallbacks(inputProvider);

            // Wire up this component for player-lifecycle callbacks.
            _runner.AddCallbacks(this);

            var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

            var result = await _runner.StartGame(new StartGameArgs
            {
                GameMode     = _gameMode,
                SessionName  = _sessionName,
                PlayerCount  = _maxPlayers,
                Scene        = scene,
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            });

            if (!result.Ok)
            {
                Debug.LogError($"[MissionLauncher] Failed to start session: {result.ShutdownReason}");
            }
        }

        // ── INetworkRunnerCallbacks – player lifecycle ───────────────────────

        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (!runner.IsServer) return;
            if (_playerPrefab == null) return;

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

        // ── INetworkRunnerCallbacks – unused stubs ───────────────────────────

        public void OnInput(NetworkRunner runner, NetworkInput input) { }
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
        public void OnConnectedToServer(NetworkRunner runner) { }
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
            => Debug.LogError($"[MissionLauncher] Connect failed: {reason}");
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
        public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
        public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
        public void OnSceneLoadDone(NetworkRunner runner) { }
        public void OnSceneLoadStart(NetworkRunner runner) { }
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
            => Debug.Log($"[MissionLauncher] Session shut down: {shutdownReason}");
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

        // ── Helpers ──────────────────────────────────────────────────────────

        private Vector2 GetSpawnPosition(PlayerRef player)
        {
            float offset = (player.AsIndex % _maxPlayers) * 1.5f;
            return new Vector2(offset - (_maxPlayers * 0.75f), 0f);
        }
    }
}
