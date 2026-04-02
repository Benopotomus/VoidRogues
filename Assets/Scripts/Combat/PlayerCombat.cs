using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Combat
{
    /// <summary>
    /// Player weapon controller. Reads fire input from <see cref="Player.PlayerInputReader"/>
    /// and fires projectiles from the pool at the configured fire rate.
    /// Damage is scaled by the current run's DamageMultiplier.
    /// </summary>
    [RequireComponent(typeof(Player.PlayerInputReader))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Weapon Stats")]
        [SerializeField] private int baseDamage   = 10;
        [SerializeField] private float fireRate   = 3f;   // shots per second
        [SerializeField] private Transform muzzle;

        [Header("Pooling")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private int poolInitialSize = 20;

        private Player.PlayerInputReader _input;
        private ObjectPool<Projectile> _pool;
        private float _fireTimer;

        private void Awake()
        {
            _input = GetComponent<Player.PlayerInputReader>();
            _pool  = new ObjectPool<Projectile>(projectilePrefab, null, poolInitialSize);
        }

        private void Update()
        {
            _fireTimer -= Time.deltaTime;

            if (_input.IsFiring && _fireTimer <= 0f)
                Fire();
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Fire()
        {
            float interval = 1f / Mathf.Max(0.01f, GetFireRate());
            _fireTimer = interval;

            Vector2 aimDir = (_input.AimInput - (Vector2)transform.position).normalized;
            if (aimDir.sqrMagnitude < 0.01f)
                aimDir = Vector2.right;

            Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position;
            Projectile p = _pool.Get(spawnPos, Quaternion.identity);
            p.OnReturnToPool = _pool.Return;
            p.Launch(GetDamage(), aimDir, isPlayerProjectile: true);
        }

        private int GetDamage()
        {
            float multiplier = GameManager.Instance != null
                ? GameManager.Instance.Run.DamageMultiplier
                : 1f;
            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        private float GetFireRate()
        {
            float multiplier = GameManager.Instance != null
                ? GameManager.Instance.Run.FireRateMultiplier
                : 1f;
            return fireRate * multiplier;
        }
    }
}
