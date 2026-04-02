using System.Collections;
using Fusion;
using UnityEngine;
using VoidRogues.Enemies;
using VoidRogues.Props;
using VoidRogues.Projectiles;

namespace VoidRogues.GameFlow
{
    /// <summary>
    /// Controls the lifecycle of a single mission: spawning managers, running waves,
    /// detecting mission end, and returning to the Ship.
    ///
    /// Attach to a NetworkObject in the Mission scene.  The host spawns this object
    /// when the scene finishes loading.
    /// </summary>
    public class MissionManager : NetworkBehaviour
    {
        [Header("Manager Prefabs")]
        [SerializeField] private NetworkObject _enemyManagerPrefab;
        [SerializeField] private NetworkObject _propsManagerPrefab;
        [SerializeField] private NetworkObject _projectileManagerPrefab;

        [Header("Wave Configuration")]
        [SerializeField] private WaveDefinition[] _waves;

        [Header("Prop Spawn Config")]
        [SerializeField] private PropSpawnEntry[] _propSpawns;

        // Runtime references
        private EnemyManager      _enemyManager;
        private PropsManager      _propsManager;
        private ProjectileManager _projectileManager;

        private int  _currentWave;
        private bool _missionComplete;

        // Cached spawn points (populated in Spawned to avoid repeated scene queries).
        private Transform[] _spawnPoints;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            if (!Runner.IsServer) return;

            CacheSpawnPoints();
            SpawnManagers();
            SpawnInitialProps();
            GameManager.Instance?.OnMissionStarted();
            StartCoroutine(RunMission());
        }

        private void CacheSpawnPoints()
        {
            var pointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");
            _spawnPoints = new Transform[pointObjects.Length];
            for (int i = 0; i < pointObjects.Length; i++)
            {
                _spawnPoints[i] = pointObjects[i].transform;
            }
        }

        private void SpawnManagers()
        {
            var em = Runner.Spawn(_enemyManagerPrefab, Vector3.zero, Quaternion.identity);
            _enemyManager = em.GetComponent<EnemyManager>();

            var pm = Runner.Spawn(_propsManagerPrefab, Vector3.zero, Quaternion.identity);
            _propsManager = pm.GetComponent<PropsManager>();

            var proj = Runner.Spawn(_projectileManagerPrefab, Vector3.zero, Quaternion.identity);
            _projectileManager = proj.GetComponent<ProjectileManager>();
        }

        private void SpawnInitialProps()
        {
            if (_propsManager == null || _propSpawns == null) return;

            foreach (var entry in _propSpawns)
            {
                _propsManager.RegisterProp(entry.TypeIndex, entry.Position);
            }
        }

        // ------------------------------------------------------------------
        // Wave coroutine (host only)
        // ------------------------------------------------------------------

        private IEnumerator RunMission()
        {
            for (_currentWave = 0; _currentWave < _waves.Length; _currentWave++)
            {
                yield return StartCoroutine(RunWave(_waves[_currentWave]));

                // Wait for all enemies to die before advancing.
                yield return new WaitUntil(() => _enemyManager == null || _enemyManager.ActiveEnemyCount == 0);

                // Small delay between waves.
                yield return new WaitForSeconds(2f);
            }

            CompleteMission();
        }

        private IEnumerator RunWave(WaveDefinition wave)
        {
            foreach (var group in wave.Groups)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    // Pick a random spawn point from the level.
                    var spawnPos = GetSpawnPoint();
                    _enemyManager.ActivateEnemy(group.EnemyTypeIndex, spawnPos);

                    if (group.SpawnIntervalSeconds > 0f)
                    {
                        yield return new WaitForSeconds(group.SpawnIntervalSeconds);
                    }
                }
            }
        }

        private void CompleteMission()
        {
            if (_missionComplete) return;
            _missionComplete = true;
            Debug.Log("[MissionManager] Mission complete.");
            GameManager.Instance?.ReturnToShip();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private Vector2 GetSpawnPoint()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                return _spawnPoints[Random.Range(0, _spawnPoints.Length)].position;
            }

            // Fallback: random offset from world origin.
            return new Vector2(Random.Range(-10f, 10f), Random.Range(-10f, 10f));
        }
    }

    // ------------------------------------------------------------------
    // Data types
    // ------------------------------------------------------------------

    /// <summary>A single wave of enemies defined in the Inspector.</summary>
    [System.Serializable]
    public class WaveDefinition
    {
        public EnemyGroup[] Groups;
    }

    /// <summary>A group of enemies of the same type spawned during a wave.</summary>
    [System.Serializable]
    public class EnemyGroup
    {
        public byte  EnemyTypeIndex;
        public int   Count;
        [Tooltip("Seconds between individual enemy spawns within this group.")]
        public float SpawnIntervalSeconds = 0.2f;
    }

    /// <summary>Initial prop placement data set in the Inspector.</summary>
    [System.Serializable]
    public class PropSpawnEntry
    {
        public byte    TypeIndex;
        public Vector2 Position;
    }
}
