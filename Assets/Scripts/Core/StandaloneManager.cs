using Fusion;
using Fusion.Photon.Realtime;
using UnityEngine;
using System.Collections;

/// This is added to scenes and will bootstrap a game mode and server type.

namespace VoidRogues
{
    using System;

    [Serializable]
    public sealed class StandaloneConfiguration
    {
        public int GameSessionID;
        public int LevelSeed;
        public GameMode GameMode;
        public string ServerName;
        public int MaxPlayers;
        public int ExtraPeers;
        public string Region;
        public string SessionName;
        public string CustomLobby;
        public string IPAddress;
        public ushort Port;
        public bool Multiplay;
        public bool QueryProtocol;
        public bool Matchmaking;
        public bool Backfill;
    }

    public class StandaloneManager : MonoBehaviour
    {
        public bool EnableStandaloneManager;

        private static StandaloneConfiguration _activeConfiguration = null;
        public static StandaloneConfiguration ActiveConfiguration { get { return _activeConfiguration; } }

        // PUBLIC MEMBERS

        public static StandaloneConfiguration ExternalConfiguration;

        // PRIVATE MEMBERS

        [SerializeField]
        private StandaloneConfiguration _defaultConfiguration;

        // MONOBEHAVIOUR

        protected IEnumerator Start()
        {
            StandaloneConfiguration configuration = ExternalConfiguration ?? _defaultConfiguration;
            _activeConfiguration = configuration;

            if(!EnableStandaloneManager)
                yield break;

            while (Global.IsInitialized == false)
                yield return null;

            var scenePath = UnityEngine.SceneManagement.SceneManager.GetActiveScene().path;

            scenePath = scenePath.Substring("Assets/".Length, scenePath.Length - "Assets/".Length - ".unity".Length);

            PhotonAppSettings.Global.AppSettings.FixedRegion = configuration.Region;

            var request = new FSessionRequest
            {
                UserID = "Steve",
                GameMode = configuration.GameMode,
                SessionName = configuration.SessionName.HasValue() ? configuration.SessionName : Guid.NewGuid().ToString(),
                DisplayName = configuration.ServerName,
                ScenePath = scenePath,
                LevelSeed = configuration.LevelSeed,
                MaxPlayers = configuration.MaxPlayers,
                CustomLobby = configuration.CustomLobby.HasValue() ? configuration.CustomLobby : "LichLord." + Application.version,
                IPAddress = configuration.IPAddress,
                Port = configuration.Port,
            };

                while (Global.Networking == null)
                    yield return null;
                Global.Networking.StartGame(request);
        }
    }
}
