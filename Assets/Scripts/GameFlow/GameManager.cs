using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidRogues.GameFlow
{
    /// <summary>
    /// Singleton that tracks the top-level game state and coordinates scene transitions.
    ///
    /// State machine:
    ///   CONNECTING → IN_SHIP → LOADING_MISSION → IN_MISSION → RETURNING → IN_SHIP
    ///
    /// This is a <see cref="NetworkBehaviour"/> so that all clients see the same
    /// <see cref="CurrentState"/> at all times.
    /// </summary>
    public class GameManager : NetworkBehaviour
    {
        // ------------------------------------------------------------------
        // Singleton
        // ------------------------------------------------------------------

        public static GameManager Instance { get; private set; }

        // ------------------------------------------------------------------
        // State
        // ------------------------------------------------------------------

        public enum GameState : byte
        {
            Connecting      = 0,
            InShip          = 1,
            LoadingMission  = 2,
            InMission       = 3,
            Returning       = 4,
        }

        [Networked]
        [OnChangedRender(nameof(OnStateChanged))]
        public GameState CurrentState { get; private set; }

        // ------------------------------------------------------------------
        // Events (local; not networked)
        // ------------------------------------------------------------------

        public static event System.Action<GameState> OnGameStateChanged;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            if (Instance != null && Instance != this)
            {
                Runner.Despawn(Object);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (Object.HasStateAuthority)
            {
                CurrentState = GameState.InShip;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------
        // Transitions (host calls these)
        // ------------------------------------------------------------------

        /// <summary>
        /// Loads a mission scene by its build index offset.
        /// </summary>
        public void LoadMission(int missionSceneIndex = 2)
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentState != GameState.InShip) return;

            CurrentState = GameState.LoadingMission;
            Runner.LoadScene(SceneRef.FromIndex(missionSceneIndex), LoadSceneMode.Single);
        }

        /// <summary>
        /// Called by <see cref="MissionManager"/> when the mission is complete.
        /// </summary>
        public void ReturnToShip()
        {
            if (!Object.HasStateAuthority) return;
            if (CurrentState != GameState.InMission) return;

            CurrentState = GameState.Returning;
            Runner.LoadScene(SceneRef.FromIndex(1), LoadSceneMode.Single); // Ship
        }

        /// <summary>Called by <see cref="MissionManager"/> once the mission scene is ready.</summary>
        public void OnMissionStarted()
        {
            if (!Object.HasStateAuthority) return;
            CurrentState = GameState.InMission;
        }

        /// <summary>Called by <see cref="ShipManager"/> once the Ship scene is fully loaded.</summary>
        public void OnShipReady()
        {
            if (!Object.HasStateAuthority) return;
            CurrentState = GameState.InShip;
        }

        // ------------------------------------------------------------------
        // Change callback
        // ------------------------------------------------------------------

        private void OnStateChanged()
        {
            OnGameStateChanged?.Invoke(CurrentState);
        }
    }
}
