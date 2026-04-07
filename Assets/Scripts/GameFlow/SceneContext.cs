using UnityEngine;
using VoidRogues.Enemies;
using VoidRogues.NPCs;
using VoidRogues.Projectiles;
using VoidRogues.Props;

namespace VoidRogues.GameFlow
{
    /// <summary>
    /// Centralized scene context that holds references to all active managers.
    ///
    /// Follows the Hallowheart / LichLord SceneContext pattern: instead of calling
    /// <c>FindObjectOfType</c> scattered throughout the codebase, systems resolve
    /// their dependencies through this single access point.
    ///
    /// Usage:
    ///   - Place one <see cref="SceneContext"/> on a persistent GameObject in each
    ///     gameplay scene (Mission, Ship, etc.).
    ///   - After all managers are spawned, call <see cref="Register"/> methods or
    ///     assign references from the spawning code (e.g. <c>MissionManager</c>).
    ///   - Other systems access managers via <c>SceneContext.Instance.EnemyManager</c>
    ///     instead of <c>FindObjectOfType&lt;EnemyManager&gt;()</c>.
    /// </summary>
    public class SceneContext : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // Singleton
        // ------------------------------------------------------------------

        public static SceneContext Instance { get; private set; }

        // ------------------------------------------------------------------
        // Manager References
        // ------------------------------------------------------------------

        /// <summary>The active <see cref="Enemies.EnemyManager"/> for this scene.</summary>
        public EnemyManager EnemyManager { get; private set; }

        /// <summary>The active <see cref="Props.PropsManager"/> for this scene.</summary>
        public PropsManager PropsManager { get; private set; }

        /// <summary>The active <see cref="Projectiles.ProjectileManager"/> for this scene.</summary>
        public ProjectileManager ProjectileManager { get; private set; }

        /// <summary>The active <see cref="NPCs.NPCManager"/> for this scene.</summary>
        public NPCManager NPCManager { get; private set; }

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

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
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------
        // Registration API
        // ------------------------------------------------------------------

        /// <summary>Registers an <see cref="Enemies.EnemyManager"/> with the scene context.</summary>
        public void RegisterEnemyManager(EnemyManager manager)
        {
            EnemyManager = manager;
        }

        /// <summary>Registers a <see cref="Props.PropsManager"/> with the scene context.</summary>
        public void RegisterPropsManager(PropsManager manager)
        {
            PropsManager = manager;
        }

        /// <summary>Registers a <see cref="Projectiles.ProjectileManager"/> with the scene context.</summary>
        public void RegisterProjectileManager(ProjectileManager manager)
        {
            ProjectileManager = manager;
        }

        /// <summary>Registers an <see cref="NPCs.NPCManager"/> with the scene context.</summary>
        public void RegisterNPCManager(NPCManager manager)
        {
            NPCManager = manager;
        }

        // ------------------------------------------------------------------
        // Convenience
        // ------------------------------------------------------------------

        /// <summary>
        /// Returns true when all essential managers have been registered.
        /// </summary>
        public bool AllManagersReady =>
            EnemyManager != null &&
            PropsManager != null &&
            ProjectileManager != null &&
            NPCManager != null;

        /// <summary>
        /// Clears all manager references. Called when tearing down a scene
        /// (e.g. returning from a mission to the ship).
        /// </summary>
        public void ClearAll()
        {
            EnemyManager      = null;
            PropsManager      = null;
            ProjectileManager = null;
            NPCManager        = null;
        }
    }
}
