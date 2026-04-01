using System.Collections.Generic;
using UnityEngine;
using VoidRogues.Core;
using VoidRogues.Enemies;

namespace VoidRogues.Dungeon
{
    /// <summary>
    /// Manages the lifecycle of a single room instance:
    /// spawns enemies, locks/unlocks doors, and triggers rewards on clear.
    /// One RoomController exists per active room prefab.
    /// </summary>
    public class RoomController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform   rewardSpawnPoint;
        [SerializeField] private GameObject[] doorObjects;

        [Header("Reward")]
        [SerializeField] private GameObject chestPrefab;

        private RoomDataSO _data;
        private int _aliveEnemyCount;
        private bool _isCleared;

        // ── Initialisation ────────────────────────────────────────────────────

        /// <summary>Initialise the room with its template and spawn content.</summary>
        public void Initialise(RoomDataSO data)
        {
            _data = data;
            SetDoorsLocked(true);
            SpawnEnemies();

            EventBus.Publish(new RoomEnteredEvent { RoomType = data.roomType });
        }

        private void OnEnable()
        {
            EventBus.Subscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<EnemyKilledEvent>(OnEnemyKilled);
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void SpawnEnemies()
        {
            if (_data.possibleEnemies == null || _data.possibleEnemies.Length == 0)
            {
                ClearRoom();
                return;
            }

            int count = Random.Range(_data.minEnemyCount, _data.maxEnemyCount + 1);
            count = Mathf.Min(count, enemySpawnPoints.Length);

            ShuffleArray(enemySpawnPoints);

            for (int i = 0; i < count; i++)
            {
                var enemyData = _data.possibleEnemies[Random.Range(0, _data.possibleEnemies.Length)];
                // Enemies are spawned via prefab reference on EnemyDataSO (set in inspector)
                Debug.Log($"[RoomController] Spawning enemy '{enemyData.enemyName}' at {enemySpawnPoints[i].position}");
            }

            _aliveEnemyCount = count;

            // If no enemies were queued, clear immediately
            if (_aliveEnemyCount <= 0)
                ClearRoom();
        }

        private void OnEnemyKilled(EnemyKilledEvent evt)
        {
            if (_isCleared)
                return;

            _aliveEnemyCount--;
            if (_aliveEnemyCount <= 0)
                ClearRoom();
        }

        private void ClearRoom()
        {
            if (_isCleared)
                return;

            _isCleared = true;
            SetDoorsLocked(false);
            SpawnReward();
            EventBus.Publish(new RoomClearedEvent());
        }

        private void SpawnReward()
        {
            if (chestPrefab != null && rewardSpawnPoint != null
                && Random.value < _data.chestSpawnChance)
            {
                Instantiate(chestPrefab, rewardSpawnPoint.position, Quaternion.identity);
            }
        }

        private void SetDoorsLocked(bool locked)
        {
            foreach (var door in doorObjects)
            {
                if (door != null)
                    door.SetActive(locked);
            }
        }

        private static void ShuffleArray<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }
    }
}
