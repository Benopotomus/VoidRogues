using DWD.Utility.Loading;
using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LogType = UnityEngine.LogType;
using VoidRogues.Players;

namespace VoidRogues
{

    [System.Serializable]
    public class GameplayModeBundle
    {
        public EGameplayModeType type = EGameplayModeType.None;
        [BundleObject(typeof(GameplayMode))]
        public BundleObject mode;
    }

    public sealed class NetworkGame : ContextBehaviour, IPlayerJoined, IPlayerLeft
    {
        // PUBLIC MEMBERS

        public List<PlayerEntity> ActivePlayers = new List<PlayerEntity>();
        public int ActivePlayerCount = 0;

        public event Action GameLoaded;

        [Space]
        [Header("Level Generation")]
        [Space]

        [SerializeField]
        private PlayerEntity _playerPrefab;

        [SerializeField]
        private GameplayModeBundle[] _gameplayModes = new GameplayModeBundle[0];

        private PlayerRef _localPlayer;
        private Dictionary<PlayerRef, PlayerEntity> _pendingPlayers = new Dictionary<PlayerRef, PlayerEntity>();
        private Dictionary<string, PlayerEntity> _disconnectedPlayers = new Dictionary<string, PlayerEntity>();
        private FusionCallbacksHandler _fusionCallbacks = new FusionCallbacksHandler();
        private List<PlayerEntity> _spawnedPlayers = new List<PlayerEntity>(byte.MaxValue);
        private List<PlayerEntity> _allPlayers = new List<PlayerEntity>(byte.MaxValue);
        //private StatsRecorder _statsRecorder;
        //private LogRecorder _logRecorder;

        // PUBLIC METHODS

        public BundleObject GetBundleObjectForType(EGameplayModeType type)
        {
            int count = _gameplayModes.Length;
            for (int a = 0; a < count; a++)
            {
                GameplayModeBundle temp = _gameplayModes[a];
                if (temp.type == type)
                    return temp.mode;
            }
            return null;
        }

        public void Initialize(EGameplayModeType gameplayModeType)
        {
            if (HasStateAuthority == true)
            {
                LoadGameplayMode(gameplayModeType);
            }

            _localPlayer = Runner.LocalPlayer;

            _fusionCallbacks.DisconnectedFromServer -= OnDisconnectedFromServer;
            _fusionCallbacks.DisconnectedFromServer += OnDisconnectedFromServer;

            Runner.RemoveCallbacks(_fusionCallbacks);
            Runner.AddCallbacks(_fusionCallbacks);

            ActivePlayers.Clear();
            ActivePlayerCount = 0;
        }

        private void LoadGameplayMode(EGameplayModeType gameplayModeType)
        {
            BundleObject modeObject = GetBundleObjectForType(gameplayModeType);

            // If the requested type is not found, fall back to the first available mode
            if (modeObject == null && _gameplayModes.Length > 0)
            {
                Debug.LogWarning($"GameplayMode type {gameplayModeType} not found, falling back to {_gameplayModes[0].type}");
                modeObject = _gameplayModes[0].mode;
            }

            if (modeObject == null)
            {
                Debug.LogError("No GameplayMode bundles configured on NetworkGame!");
                return;
            }

            if (modeObject.Ready)
            {
                AssetBundleLoader modeLoader = AssetBundleManager.Instance.LoadBundleObject(modeObject) as AssetBundleLoader;
                if (modeLoader.IsLoaded)
                    OnGameplayModeLoaded(modeLoader);
                else
                {
                    modeLoader.OnLoadComplete += OnGameplayModeLoaded;
                }
            }
        }

        private void OnGameplayModeLoaded(ILoader loader)
        {
            AssetBundleLoader modeLoader = loader as AssetBundleLoader;
            modeLoader.OnLoadComplete -= OnGameplayModeLoaded;

            GameObject prefab = modeLoader.GetAsset<GameObject>();
            if (prefab == null)
                prefab = modeLoader.GetAssetWithin<GameObject>();

            if (prefab == null)
            {
                Debug.LogError("GameplayMode prefab failed to load from asset bundle!");
                return;
            }

            var modeInstance = Runner.Spawn(prefab);
        }

        public IEnumerator Activate(int levelIndex, int levelGeneratorSeed)
        {
            if (HasStateAuthority)
            {
                Debug.Log("Spawning Players");
                foreach (var playerRef in Runner.ActivePlayers)
                {
                    if (PlayerEntity.GetPlayerEntity(Runner, playerRef) != null || _pendingPlayers.ContainsKey(playerRef))
                        continue;

                    SpawnPlayer(playerRef);
                }
            }

            if (ApplicationSettings.IsStrippedBatch == true)
            {
                //Runner.GetComponent<RunnerSimulatePhysics2D>().enabled = false;
                //Runner.LagCompensation.enabled = false;
            }

            while (Context.GameplayMode == null)
                yield return null;

            //while (Context.LevelManager == null)
            //    yield return null;

            Debug.Log("Activating Game Mode");
            Context.GameplayMode.Activate();

            //while (Context.LevelManager.LoadedLevel == null)
            //    yield return null;

            GameLoaded?.Invoke();
        }

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            Runner.SetIsSimulated(Object, true);
        }

        public override void FixedUpdateNetwork()
        {
            bool hasStateAuthority = HasStateAuthority;

            _allPlayers.Clear();
            Runner.GetAllBehaviours<PlayerEntity>(_allPlayers);

            for (int i = _allPlayers.Count - 1; i >= 0; --i)
            {
                PlayerEntity player = _allPlayers[i];

                PlayerRef inputAuthority = player.Object.InputAuthority;
                if (inputAuthority.IsRealPlayer == true)
                {
                    if (hasStateAuthority == true && Runner.IsPlayerValid(inputAuthority) == false)
                    {
                        _allPlayers.RemoveAt(i);
                        OnPlayerLeft(player);
                    }
                }
                else
                {
                    _allPlayers.RemoveAt(i);
                }
            }

            ActivePlayers.Clear();
            ActivePlayerCount = 0;

            foreach (PlayerEntity player in _allPlayers)
            {
                if (player.UserID.HasValue() == false)
                    continue;

                ActivePlayers.Add(player);

                var statistics = player.Statistics;
                if (statistics.IsValid == false)
                    continue;

                if (statistics.IsEliminated == false)
                {
                    ++ActivePlayerCount;
                }
            }

            if (_pendingPlayers.Count == 0)
                return;

            var playersToRemove = ListPool.Get<PlayerRef>(32);

            foreach (var playerPair in _pendingPlayers)
            {
                var playerRef = playerPair.Key;
                PlayerEntity player = playerPair.Value;

                if (player.IsInitialized == false)
                    continue;

                playersToRemove.Add(playerRef);

                if (_disconnectedPlayers.TryGetValue(player.UserID, out PlayerEntity disconnectedPlayer) == true)
                {
                    _disconnectedPlayers.Remove(player.UserID);

                    int activePlayerIndex = ActivePlayers.IndexOf(player);
                    if (activePlayerIndex >= 0)
                    {
                        ActivePlayers[activePlayerIndex] = disconnectedPlayer;
                    }

                    disconnectedPlayer.OnReconnect(player);

                    // Remove original player, this is returning disconnected player
                    player.Object.RemoveInputAuthority();
                    Runner.Despawn(player.Object);

                    player = disconnectedPlayer;
                    player.Object.AssignInputAuthority(playerRef);
                    //player.RefreshPlayerProperties();
                    Runner.SetPlayerAlwaysInterested(playerRef, player.Object, true);
                }

                player.Refresh();
                Runner.SetPlayerObject(playerRef, player.Object);

#if UNITY_EDITOR
                player.gameObject.name = $"Player {player.Nickname}";
#endif
                if (Context.GameplayMode != null)
                    Context.GameplayMode.OnPlayerJoined(player);
            }

            for (int i = 0; i < playersToRemove.Count; i++)
            {
                _pendingPlayers.Remove(playersToRemove[i]);
            }

            ListPool.Return(playersToRemove);
        }

        // IPlayerJoined/IPlayerLeft INTERFACES

        void IPlayerJoined.PlayerJoined(PlayerRef playerRef)
        {
            if (!HasStateAuthority)
                return;

            if (PlayerEntity.GetPlayerEntity(Runner, playerRef) != null || _pendingPlayers.ContainsKey(playerRef))
                return;

            SpawnPlayer(playerRef);
        }

        void IPlayerLeft.PlayerLeft(PlayerRef playerRef)
        {
            if (playerRef.IsRealPlayer == false)
                return;
            if (!HasStateAuthority)
                return;

            OnPlayerLeft(PlayerEntity.GetPlayerEntity(Runner, playerRef));
        }

        private void OnPlayerLeft(PlayerEntity player)
        {
            if (player == null)
                return;

            ActivePlayers.Remove(player);

            if (player.UserID.HasValue() == true)
            {
                _disconnectedPlayers[player.UserID] = player;

                if (Context != null)
                    Context.GameplayMode.OnPlayerLeft(player);

                player.Object.RemoveInputAuthority();

#if UNITY_EDITOR
                player.gameObject.name = $"{player.gameObject.name} (Disconnected)";
#endif
            }
            else
            {
                Context.GameplayMode.OnPlayerLeft(player);

                // Player wasn't initilized properly, safe to despawn
                Runner.Despawn(player.Object);
            }
        }

        // MonoBehaviour INTERFACE

        private void Update()
        {
            if (ApplicationSettings.RecordSession == false)
                return;
            if (Object == null)
                return;
            /*
            if (_statsRecorder == null)
            {
                string fileID = $"{System.DateTime.Now:yyyy-MM-dd-HH-mm-ss}";

                string statsFileName = $"FusionBR_{fileID}_Stats.log";
                string logFileName = $"FusionBR_{fileID}_Log.log";

                _statsRecorder = new StatsRecorder();
                _statsRecorder.Initialize(ApplicationUtility.GetFilePath(statsFileName), fileID, "Time", "Players", "DeltaTime");

                _logRecorder = new LogRecorder();
                _logRecorder.Initialize(ApplicationUtility.GetFilePath(logFileName));
                _logRecorder.Write(fileID);

                Application.logMessageReceived -= OnLogMessage;
                Application.logMessageReceived += OnLogMessage;

                PrintInfo();
            }

            string time = Time.realtimeSinceStartup.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string players = ActivePlayerCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            string deltaTime = (Time.deltaTime * 1000.0f).ToString(System.Globalization.CultureInfo.InvariantCulture);

            _statsRecorder.Write(time, players, deltaTime);
            */
        }

        private void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            /*
            if (_logRecorder == null)
                return;

            _logRecorder.Write(condition);

            if (type == LogType.Exception)
            {
                _logRecorder.Write(stackTrace);
            }
            */
        }

        private void OnDestroy()
        {
            /*
            if (_statsRecorder != null)
            {
                _statsRecorder.Deinitialize();
                _statsRecorder = null;
            }

            if (_logRecorder != null)
            {
                _logRecorder.Deinitialize();
                _logRecorder = null;
            }
            */
        }

        // PRIVATE METHODS

        private void SpawnPlayer(PlayerRef playerRef)
        {
            if (PlayerEntity.GetPlayerEntity(Runner, playerRef) != null || _pendingPlayers.ContainsKey(playerRef) == true)
            {
                Log.Warn($"Player for {playerRef} is already spawned!");
                return;
            }

            var player = Runner.Spawn(_playerPrefab, inputAuthority: playerRef);

            Runner.SetPlayerAlwaysInterested(playerRef, player.Object, true);

            _pendingPlayers[playerRef] = player;

#if UNITY_EDITOR
            player.gameObject.name = $"Player Unknown (Pending)";
#endif
        }

        private void PrintInfo()
        {
            Debug.Log($"ApplicationUtility.DataPath: {ApplicationUtility.DataPath}");
            Debug.Log($"Environment.CommandLine: {Environment.CommandLine}");
            Debug.Log($"SystemInfo.deviceModel: {SystemInfo.deviceModel}");
            Debug.Log($"SystemInfo.deviceName: {SystemInfo.deviceName}");
            Debug.Log($"SystemInfo.deviceType: {SystemInfo.deviceType}");
            Debug.Log($"SystemInfo.processorCount: {SystemInfo.processorCount}");
            Debug.Log($"SystemInfo.processorFrequency: {SystemInfo.processorFrequency}");
            Debug.Log($"SystemInfo.processorType: {SystemInfo.processorType}");
            Debug.Log($"SystemInfo.systemMemorySize: {SystemInfo.systemMemorySize}");
        }

        // NETWORK CALLBACKS

        private void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (runner != null)
            {
                //runner.SetLocalPlayer(_localPlayer);
            }

            Debug.Log("OnDisconnectedFromServer");
        }
    }
}
