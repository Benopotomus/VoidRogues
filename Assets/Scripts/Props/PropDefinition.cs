using UnityEngine;

namespace VoidRogues.Props
{
    /// <summary>
    /// ScriptableObject that defines a single destructible prop type.
    ///
    /// Create via: Assets → Create → VoidRogues → Prop Definition
    /// </summary>
    [CreateAssetMenu(menuName = "VoidRogues/Prop Definition", fileName = "NewProp")]
    public class PropDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string PropName = "Unnamed Prop";

        [Header("Stats")]
        public short MaxHealth       = 30;
        public bool  IsExplosive     = true;
        public float ExplosionRadius = 2.5f;
        public int   ExplosionDamage = 50;

        [Header("Collision")]
        [Tooltip("Radius used by Physics2D overlap checks.")]
        public float ColliderRadius = 0.35f;

        [Header("Visuals")]
        [Tooltip("Non-networked visual prefab managed by PropsManager.")]
        public GameObject VisualPrefab;
        public ParticleSystem ExplosionVFXPrefab;
    }
}
