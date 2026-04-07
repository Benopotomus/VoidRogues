using System;
using System.Collections;
using UnityEngine;
using DWD.Utility.Loading;
using Fusion;

namespace LichLord
{
    /// <summary>
    /// Manages global resources and initialization, persisting across all scenes.
    /// Automatically created at runtime and remains active for the entire game.
    /// </summary>
    public class Global : MonoBehaviour
    {
        // PUBLIC MEMBERS
        public static GlobalSettings Settings { get; private set; }
        public static GlobalTables Tables { get; private set; }
        public static RuntimeSettings RuntimeSettings { get; private set; }
        public static Networking Networking { get; private set; }
        public static bool IsInitialized { get; private set; }
        public static event Action OnInitialized;

        // PRIVATE MEMBERS
        private const string SETTINGS_BUNDLE = "globalsettings";
        private const string TABLES_BUNDLE = "globaltables";
        private static Global _instance;

        [SerializeField] private AssetBundleManager _assetBundleManager;
        // PUBLIC METHODS

        private void Awake()
        {
            // Ensure only one Global instance exists
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            StartCoroutine(InitializeCoroutine());
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Deinitialize();
                _instance = null;
            }
        }

        /// <summary>
        /// Quits the application, cleaning up global resources.
        /// </summary>
        public static void Quit()
        {
            if (Application.isPlaying)
            {
                Deinitialize();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
           Application.Quit();
#endif
            }
        }

        // PRIVATE METHODS
        /// <summary>
        /// Creates a Global instance if none exists.
        /// </summary>
        private static void EnsureInstance()
        {
            if (_instance == null)
            {
                var go = new GameObject(nameof(Global));
                _instance = go.AddComponent<Global>();
            }
        }

        /// <summary>
        /// Initializes global resources by loading asset bundles.
        /// </summary>
        private IEnumerator InitializeCoroutine()
        {
            Instantiate(_assetBundleManager);

            // Wait for AssetBundleManager to be ready
            while (AssetBundleManager.Instance == null || !AssetBundleManager.Instance.ReadyToLoad)
                yield return null;

            // Load settings bundle
            var settingsLoader = AssetBundleManager.Instance.LoadBundle(SETTINGS_BUNDLE, false) as AssetBundleLoader;
            while (!settingsLoader.IsLoaded)
                yield return null;
            Settings = settingsLoader.GetAsset<GlobalSettings>();
            if (Settings == null)
            {
                Debug.LogError($"Failed to load {SETTINGS_BUNDLE} asset.", this);
                yield break;
            }

            // Load tables bundle
            var tableLoader = AssetBundleManager.Instance.LoadBundle(TABLES_BUNDLE, false) as AssetBundleLoader;
            while (!tableLoader.IsLoaded)
                yield return null;
            Tables = tableLoader.GetAsset<GlobalTables>();
            if (Tables == null)
            {
                Debug.LogError($"Failed to load {TABLES_BUNDLE} asset.", this);
                yield break;
            }

            // Initialize runtime settings and networking
            RuntimeSettings = new RuntimeSettings();
            RuntimeSettings.Initialize(Settings);
            Networking = CreateStaticObject<Networking>();

            // Mark as initialized
            IsInitialized = true;
            OnInitialized?.Invoke();
        }

        /// <summary>
        /// Cleans up global resources.
        /// </summary>
        private static void Deinitialize()
        {
            if (!IsInitialized)
                return;

            IsInitialized = false;
            Settings = null;
            Tables = null;
            RuntimeSettings = null;
            Networking = null;
            OnInitialized = null; // Clear event handlers
        }

        /// <summary>
        /// Creates a persistent GameObject with the specified component.
        /// </summary>
        private static T CreateStaticObject<T>() where T : Component
        {
            var gameObject = new GameObject(typeof(T).Name);
            DontDestroyOnLoad(gameObject);
            return gameObject.AddComponent<T>();
        }
    }
}