using UnityEngine;

namespace VoidRogues.Player
{
    /// <summary>
    /// ScriptableObject that defines a single weapon's statistics.
    ///
    /// Create via: Assets → Create → VoidRogues → Weapon Definition
    /// </summary>
    [CreateAssetMenu(menuName = "VoidRogues/Weapon Definition", fileName = "NewWeapon")]
    public class WeaponDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string WeaponName = "Unnamed Weapon";

        /// <summary>Byte index used inside <see cref="VoidRogues.Projectiles.ProjectileState"/>.</summary>
        public byte WeaponTypeIndex;

        [Header("Fire Stats")]
        [Tooltip("Shots per second.")]
        public float FireRate = 8f;

        [Tooltip("Number of projectiles per shot (>1 for shotgun spread).")]
        public int ProjectileCount = 1;

        [Tooltip("Total spread cone in degrees (split evenly around aim direction).")]
        public float SpreadDegrees = 0f;

        [Header("Projectile")]
        public float ProjectileSpeed    = 20f;
        public float ProjectileRadius   = 0.05f;
        public int   Damage             = 10;
        [Tooltip("How many Fusion ticks the projectile travels before auto-expiring. 0 = use ProjectileManager default.")]
        public int   MaxLifetimeTicks   = 0;

        [Header("VFX")]
        public Sprite       ProjectileSprite;
        public ParticleSystem MuzzleFlashPrefab;
        public ParticleSystem HitVFXPrefab;
    }
}
