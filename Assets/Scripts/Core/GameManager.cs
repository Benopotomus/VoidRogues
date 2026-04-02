using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRogues.Core
{
    /// <summary>
    /// Persistent singleton that owns the current <see cref="RunData"/> and
    /// coordinates high-level game state (menu → run → game over).
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        public RunData Run { get; private set; }

        /// <summary>
        /// Cached reference to the player Transform.
        /// Set by the player's Awake; read by enemies and other systems.
        /// </summary>
        public Transform PlayerTransform { get; set; }

        private const string SCENE_MAIN_MENU = "MainMenu";
        private const string SCENE_GAME      = "Game";
        private const string SCENE_GAME_OVER = "GameOver";

        protected override void Awake()
        {
            base.Awake();
            Run = new RunData();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Start a fresh run: reset state and load the game scene.</summary>
        public void StartNewRun()
        {
            Run.Reset();
            SceneManager.LoadScene(SCENE_GAME);
        }

        /// <summary>Return to the main menu without persisting run data.</summary>
        public void GoToMainMenu()
        {
            SceneManager.LoadScene(SCENE_MAIN_MENU);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void OnPlayerDeath(PlayerDeathEvent evt)
        {
            Debug.Log("[GameManager] Player died. Loading game-over screen.");
            SceneManager.LoadScene(SCENE_GAME_OVER);
        }
    }
}
