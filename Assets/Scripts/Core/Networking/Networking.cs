#define ENABLE_LOGS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using UnityScene = UnityEngine.SceneManagement.Scene;
using UnityEngine.EventSystems;

namespace VoidRogues
{
    public class Networking : MonoBehaviour
    {
        // CONSTANTS
        public const string DISPLAY_NAME_KEY = "name";
        public const string LEVEL_ID = "levelId";
        public const string LEVEL_PHASE = "levelPhase";
        public const string LEVEL_SEED = "levelSeed";
        public const string TYPE_KEY = "type";
        public const string GAME_SESSION_ID = "gameSessionId";
        public const string MODE_KEY = "mode";
        public const string STATUS_SERVER_CLOSED = "Server Closed";

        // PUBLIC MEMBERS
        public string Status { get; private set; }
        public string StatusDescription { get; private set; }
        public string ErrorStatus { get; private set; }

        public bool HasSession => HasActiveSession || IsStartingSession;
        public bool IsConnecting => IsStartingSession || (HasActiveSession && !IsConnected);
        public bool IsConnected => HasActiveSession && !IsStartingSession && _runner != null && _runner.IsRunning && (_gameMode == GameMode.Single || _runner.IsConnectedToServer);

        public string ReturnScene;

        // PRIVATE MEMBERS
        private bool HasActiveSession;
        private bool IsStartingSession;
        private bool _connectionRequested;
        private NetworkSceneInfo _sceneInfo;
        private SceneContext _sceneContext;
        private GameMode _gameMode;
        private NetworkRunner _runner;
        private NetworkSceneManager _sceneManager;
        private UnityScene _loadedScene;
        private string _userID;
        private FSessionRequest _request;
        private bool _stopGameOnDisconnect;
        private Coroutine _coroutine;

        private string _cachedSessionName;
        public string SessionName => !string.IsNullOrEmpty(_cachedSessionName) ? _cachedSessionName : "SinglePlayer";

        // PUBLIC METHODS
        public void StartGame(FSessionRequest request)
        {
            _cachedSessionName = request.SessionName;
            if (_gameMode == GameMode.Single && string.IsNullOrEmpty(_cachedSessionName))
            {
                _cachedSessionName = "SinglePlayer";
            }

            if (request.GameMode != GameMode.Shared && request.GameMode != GameMode.Single)
            {
                Debug.LogError($"Only GameMode.Shared and GameMode.Single are supported. Got: {request.GameMode}");
                return;
            }

            SceneRef sceneRef = default;
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath(request.ScenePath);
            if (sceneIndex >= 0)
            {
                sceneRef = SceneRef.FromIndex(sceneIndex);
            }

            NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
            if (sceneRef.IsValid)
            {
                sceneInfo.AddSceneRef(sceneRef, LoadSceneMode.Additive);
            }

            _sceneInfo = sceneInfo;
            _gameMode = request.GameMode;
            _userID = request.UserID;
            _request = request;
            _connectionRequested = true;
            IsStartingSession = true;
            _stopGameOnDisconnect = false;
            ErrorStatus = null;

            Log($"StartGame() UserID:{request.UserID} GameMode:{request.GameMode} " +
                $"DisplayName:{request.DisplayName} SessionName:{request.SessionName} ScenePath:{request.ScenePath} " +
                $" MaxPlayers:{request.MaxPlayers} CustomLobby:{request.CustomLobby}");
        }

        public void StopGame(string errorStatus = null)
        {
            Log($"StopGame()");

            IsStartingSession = false;
            _connectionRequested = false;
            _stopGameOnDisconnect = false;

            if (HasActiveSession)
            {
                _connectionRequested = false;
            }

            ErrorStatus = errorStatus;
        }

        public void StopGameOnDisconnect()
        {
            Log($"StopGameOnDisconnect()");
            _stopGameOnDisconnect = true;
        }

        public void ClearErrorStatus()
        {
            ErrorStatus = null;
        }

        // MONOBEHAVIOUR
        protected void Update()
        {
            if (IsStartingSession)
            {
                if (!HasActiveSession)
                {
                    HasActiveSession = true;
                    IsStartingSession = false;
                }
                else
                {
                    _connectionRequested = false;
                }
            }

            UpdateSession();

            if (_coroutine == null && HasActiveSession && !IsConnected)
            {
                if (!IsStartingSession)
                {
                    Log($"Starting LoadMenuCoroutine()");
                    //_coroutine = StartCoroutine(LoadSceneCoroutine(Global.Settings.Scenes.BaseCampScene));
                }

                HasActiveSession = false;
                ClearSession();
            }
        }

        // PRIVATE METHODS
        private void UpdateSession()
        {
            if (!HasActiveSession)
            {
                Status = string.Empty;
                StatusDescription = string.Empty;
                return;
            }

            if (_coroutine != null)
                return;

            if (_stopGameOnDisconnect && _connectionRequested && !IsConnected)
            {
                Log($"Stopping game after disconnect");
                _stopGameOnDisconnect = false;
                StopGame();
                return;
            }

            if (_connectionRequested && !IsConnected)
            {
                Status = "Starting";
                Log($"Starting ConnectCoroutine()");
                _coroutine = StartCoroutine(ConnectCoroutine());
            }
            else if (!_connectionRequested && IsConnected)
            {
                Status = "Quitting";
                Log($"Starting DisconnectCoroutine()");
                _coroutine = StartCoroutine(DisconnectCoroutine());
            }
            else if (!IsConnected)
            {
                Status = "Connection Lost";
                Log($"Starting DisconnectCoroutine()");
                _coroutine = StartCoroutine(DisconnectCoroutine());
            }
        }

        private IEnumerator ConnectCoroutine(float connectionTimeout = 20f, float loadTimeout = 45f)
        {
            StatusDescription = "Unloading current scene";

            UnityScene activeScene = SceneManager.GetActiveScene();

            if (!IsSameScene(activeScene.path, _request.ScenePath))
            {
                NetworkedScene currentScene = activeScene.GetComponent<NetworkedScene>();
                if (currentScene != null)
                {
                    Log($"Deinitializing Scene");
                    currentScene.Deinitialize();
                }

                Log($"Unloading scene {activeScene.name}");
                yield return SceneManager.UnloadSceneAsync(activeScene);
                yield return null;
            }

            float baseTime = Time.realtimeSinceStartup;
            float limitTime = baseTime + connectionTimeout;

            StatusDescription = "Starting network connection";
            yield return null;

            NetworkObjectPool pool = new NetworkObjectPool();
            NetworkRunner runner = Instantiate(Global.Settings.RunnerPrefab);
            runner.name = $"Runner_{_gameMode}";
            runner.ProvideInput = true;
            runner.EnableVisibilityExtension();

            _runner = runner;
            _sceneManager = runner.GetComponent<NetworkSceneManager>();

            StartGameArgs startGameArgs = new StartGameArgs
            {
                GameMode = _gameMode,
                SessionName = _gameMode == GameMode.Shared ? _request.SessionName : null,
                Scene = _sceneInfo,
                OnGameStarted = OnGameInitialized,
                ObjectProvider = pool,
                CustomLobbyName = _gameMode == GameMode.Shared ? _request.CustomLobby : null,
                SceneManager = _sceneManager,
                EnableClientSessionCreation = _gameMode == GameMode.Shared
            };

            if (_request.MaxPlayers > 0 && _gameMode == GameMode.Shared)
            {
                startGameArgs.PlayerCount = _request.MaxPlayers;
            }

            if (_gameMode == GameMode.Shared)
            {
                startGameArgs.SessionProperties = CreateSessionProperties(_request);
            }

            if (!string.IsNullOrEmpty(_request.IPAddress) && _gameMode == GameMode.Shared)
            {
                startGameArgs.Address = NetAddress.CreateFromIpPort(_request.IPAddress, _request.Port);
            }
            else if (_request.Port > 0 && _gameMode == GameMode.Shared)
            {
                startGameArgs.Address = NetAddress.Any(_request.Port);
            }

            Log($"NetworkRunner.StartGame()");
            Task<StartGameResult> startGameTask = runner.StartGame(startGameArgs);

            while (!startGameTask.IsCompleted)
            {
                yield return null;
                if (Time.realtimeSinceStartup >= limitTime)
                {
                    Debug.LogError($"Runner start timeout! IsCompleted: {startGameTask.IsCompleted}");
                    ErrorStatus = "Connection Timeout";
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }

                if (!_connectionRequested)
                {
                    Log($"Stopping coroutine (requested by user)");
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }
            }

            StartGameResult result = startGameTask.Result;
            Log($"StartGame() Result: {result}");

            if (!result.Ok)
            {
                Debug.LogError($"Runner failed to start! Result: {result}");
                ErrorStatus = result.ShutdownReason == ShutdownReason.GameNotFound ? STATUS_SERVER_CLOSED : StringToLabel(result.ShutdownReason.ToString());
                yield return DisconnectCoroutine();
                _coroutine = null;
                yield break;
            }

            limitTime += loadTimeout;
            StatusDescription = "Waiting for server connection";

            while (!IsConnected)
            {
                yield return null;
                if (Time.realtimeSinceStartup >= limitTime)
                {
                    Debug.LogError($"Runner connection timeout!");
                    ErrorStatus = "Connection Timeout";
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }
            }

            Log($"Loading gameplay scene");
            StatusDescription = "Loading gameplay scene";

            while (!_runner.SimulationUnityScene.IsValid() || !_runner.SimulationUnityScene.isLoaded)
            {
                Log($"Waiting for NetworkRunner.SimulationUnityScene");
                yield return null;
                if (Time.realtimeSinceStartup >= limitTime)
                {
                    Debug.LogError($"Scene load timeout!");
                    ErrorStatus = "Scene Load Timeout";
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }
            }

            _loadedScene = _runner.SimulationUnityScene;
            SceneManager.SetActiveScene(_loadedScene);

            StatusDescription = "Waiting for gameplay scene load";

            var scene = _sceneManager.GameplayScene;
            while (scene == null)
            {
                Log($"Waiting for GameplayScene");
                yield return null;
                scene = _sceneManager.GameplayScene;
                if (Time.realtimeSinceStartup >= limitTime)
                {
                    Debug.LogError($"GameplayScene query timeout!");
                    ErrorStatus = "Gameplay Scene Timeout";
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }
            }

            Log($"Scene.PrepareContext()");
            scene.PrepareContext();

            var sceneContext = scene.Context;
            sceneContext.IsVisible = true;
            sceneContext.HasInput = true;
            sceneContext.Runner = _runner;
            sceneContext.PeerUserID = _userID;

            _sceneContext = sceneContext;
            pool.Context = sceneContext;

            StatusDescription = "Waiting for networked game";

            var networkGame = scene.GetComponentInChildren<NetworkGame>(true);
            while (networkGame.Object == null)
            {
                Log($"Waiting for NetworkGame");
                yield return null;
                if (Time.realtimeSinceStartup >= limitTime)
                {
                    Debug.LogError($"Network game timeout!");
                    ErrorStatus = "Network Game Timeout";
                    yield return DisconnectCoroutine();
                    _coroutine = null;
                    yield break;
                }
            }

            StatusDescription = "Activating scene";

            Log($"Scene.Initialize()");
            scene.Initialize();

            Log($"Scene.Activate()");
            yield return scene.Activate();

            StatusDescription = "Activating network game";

            Log($"NetworkGame.Activate()");
            yield return networkGame.Activate(_request.LevelSequenceID, _request.LevelSeed);

            Debug.Log($"Session started in {(Time.realtimeSinceStartup - baseTime):0.00}s");
            _coroutine = null;
        }

        private IEnumerator DisconnectCoroutine()
        {
            StatusDescription = "Disconnecting from server";

            UnityScene gameplayScene = HasActiveSession ? _loadedScene : default;
            NetworkedScene scene = gameplayScene.GetComponent<NetworkedScene>(true);

            if (gameplayScene.IsValid())
            {
                try
                {
                    Log($"Deinitializing Scene");
                    scene.Deinitialize();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            Task shutdownTask = null;
            if (HasActiveSession && _runner != null)
            {
                Debug.Log($"Shutdown {_runner.name}");
                try
                {
                    if (_gameMode == GameMode.Shared && _runner.SessionInfo != null)
                    {
                        Log($"Closing the room");
                        _runner.SessionInfo.IsOpen = false;
                        _runner.SessionInfo.IsVisible = false;
                    }
                    shutdownTask = _runner.Shutdown(true);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }

            if (shutdownTask != null)
            {
                float operationTimeout = 10f;
                while (operationTimeout > 0f && !shutdownTask.IsCompleted)
                {
                    yield return null;
                    operationTimeout -= Time.unscaledDeltaTime;
                }
            }

            StatusDescription = "Unloading gameplay scene";
            yield return null;

            if (gameplayScene.IsValid())
            {
                Debug.Log($"Unloading scene {gameplayScene.name}");
                yield return SceneManager.UnloadSceneAsync(gameplayScene);
                yield return null;
            }

            if (HasActiveSession)
            {
                ClearSession();
                HasActiveSession = false;
            }

            _coroutine = null;
            Log($"DisconnectCoroutine() finished");
        }

        private IEnumerator LoadSceneCoroutine(string sceneName)
        {
            if (SceneManager.sceneCount == 1 && SceneManager.GetSceneAt(0).name == sceneName)
            {
                _coroutine = null;
                yield break;
            }

            StatusDescription = "Loading menu scene";
            yield return null;

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
            _coroutine = null;
        }

        private void OnGameInitialized(NetworkRunner runner)
        {
            Camera camera = runner.SimulationUnityScene.FindMainCamera();
            if (camera != null)
            {
                camera.gameObject.SetActive(false);
            }

            EventSystem eventSystem = runner.SimulationUnityScene.GetComponent<EventSystem>(true);
            if (eventSystem != null)
            {
                eventSystem.gameObject.SetActive(false);
            }
        }

        public Dictionary<string, SessionProperty> CreateSessionProperties(FSessionRequest request)
        {
            var dictionary = new Dictionary<string, SessionProperty>
            {
                { DISPLAY_NAME_KEY, request.DisplayName },
                { LEVEL_ID, request.LevelSequenceID },
                { LEVEL_SEED, request.LevelSeed },
                { MODE_KEY, (int)request.GameMode },
                { GAME_SESSION_ID, request.GameSessionID }
            };
            return dictionary;
        }

        private void ClearSession()
        {
            _runner = null;
            _sceneManager = null;
            _loadedScene = default;
            _sceneContext = default;
            _sceneInfo = default;
            _gameMode = default;
            _userID = null;
            _request = default;
            _connectionRequested = false;
        }

        [System.Diagnostics.Conditional("ENABLE_LOGS")]
        private void Log(string message)
        {
            Debug.Log($"[{Time.realtimeSinceStartup:F3}][{Time.frameCount}] Networking({GetInstanceID()}): {message}");
        }

        private static string StringToLabel(string myString)
        {
            var label = System.Text.RegularExpressions.Regex.Replace(myString, "(?<=[A-Z])(?=[A-Z][a-z])", " ");
            label = System.Text.RegularExpressions.Regex.Replace(label, "(?<=[^A-Z])(?=[A-Z])", " ");
            return label;
        }

        private static bool IsSameScene(string assetPath, string scenePath)
        {
            return assetPath == $"Assets/{scenePath}.unity";
        }
    }
}