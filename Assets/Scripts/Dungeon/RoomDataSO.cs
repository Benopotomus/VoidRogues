using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Dungeon
{
    /// <summary>
    /// ScriptableObject template that describes the configuration of a room type.
    /// Designers fill these out to control the feel and content of each room category.
    /// </summary>
    [CreateAssetMenu(fileName = "RoomData_New", menuName = "VoidRogues/Room Data")]
    public class RoomDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string roomName;
        public RoomType roomType;

        [Header("Prefab")]
        [Tooltip("The prefab instantiated for this room layout.")]
        public GameObject roomPrefab;

        [Header("Enemies")]
        [Tooltip("Enemy data assets that can spawn in this room.")]
        public Enemies.EnemyDataSO[] possibleEnemies;
        public int minEnemyCount = 2;
        public int maxEnemyCount = 5;

        [Header("Rewards")]
        [Range(0f, 1f)]
        public float chestSpawnChance = 0.4f;
    }
}
