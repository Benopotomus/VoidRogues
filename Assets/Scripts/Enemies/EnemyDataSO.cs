using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// ScriptableObject definition for a single enemy type.
    /// One asset per enemy variant. Stats are read-only at runtime
    /// — copy values to a mutable state object, never modify this SO.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData_New", menuName = "VoidRogues/Enemy Data")]
    public class EnemyDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        public EnemyArchetype archetype;

        [Header("Stats")]
        public int maxHealth        = 50;
        public int contactDamage    = 10;
        public float moveSpeed      = 2.5f;
        public float attackRate     = 1f;    // attacks per second
        public int projectileDamage = 8;

        [Header("Loot")]
        [Range(0f, 1f)]
        public float itemDropChance   = 0.1f;
        public int fragmentDropMin    = 3;
        public int fragmentDropMax    = 10;

        [Header("Difficulty Scaling")]
        public float healthScalePerSector   = 1.25f;
        public float damageScalePerSector   = 1.15f;
    }

    public enum EnemyArchetype
    {
        Grunt,
        Shooter,
        Charger,
        Shielder,
        Elite,
        Boss
    }
}
