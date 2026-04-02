using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// ScriptableObject that defines a single enemy type's statistics and assets.
    ///
    /// Create via: Assets → Create → VoidRogues → Enemy Definition
    /// </summary>
    [CreateAssetMenu(menuName = "VoidRogues/Enemy Definition", fileName = "NewEnemy")]
    public class EnemyDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string EnemyName = "Unnamed Enemy";

        [Header("Stats")]
        public short MaxHealth    = 50;
        public float MoveSpeed    = 2.5f;
        public int   AttackDamage = 10;
        public float AttackRange  = 0.6f;
        public float AttackRate   = 1f;   // attacks per second
        public int   ScoreValue   = 10;

        [Header("Collision")]
        [Tooltip("Radius used for Physics2D overlap checks in EnemyManager.")]
        public float ColliderRadius = 0.3f;

        [Header("Visuals")]
        [Tooltip("The non-networked visual prefab spawned by EnemyManager on all clients.")]
        public GameObject VisualPrefab;
        public RuntimeAnimatorController AnimatorController;
    }
}
