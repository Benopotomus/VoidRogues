using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Combat
{
    /// <summary>
    /// Represents a single projectile in flight.
    /// Returned to its pool on impact or when it leaves the valid range.
    /// Pooled via <see cref="ObjectPool{T}"/>; never Destroy directly.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed        = 12f;
        [SerializeField] private float maxLifetime  = 3f;

        private int _damage;
        private bool _isPlayerProjectile;
        private float _lifetime;
        private Rigidbody2D _rb;

        // Pooling: this delegate is set by the weapon that spawns this projectile.
        public System.Action<Projectile> OnReturnToPool;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            _lifetime = 0f;
        }

        /// <summary>
        /// Initialise the projectile after retrieving it from the pool.
        /// </summary>
        /// <param name="damage">Damage dealt on hit.</param>
        /// <param name="direction">Normalised travel direction.</param>
        /// <param name="isPlayerProjectile">True if fired by the player.</param>
        public void Launch(int damage, Vector2 direction, bool isPlayerProjectile)
        {
            _damage             = damage;
            _isPlayerProjectile = isPlayerProjectile;
            _rb.linearVelocity  = direction.normalized * speed;
        }

        private void Update()
        {
            _lifetime += Time.deltaTime;
            if (_lifetime >= maxLifetime)
                ReturnToPool();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable == null)
                return;

            // Only hit the correct faction
            bool targetIsEnemy = other.gameObject.layer == LayerMask.NameToLayer("Enemy");
            bool targetIsPlayer = other.gameObject.layer == LayerMask.NameToLayer("Player");

            if ((_isPlayerProjectile && targetIsEnemy) ||
                (!_isPlayerProjectile && targetIsPlayer))
            {
                damageable.ApplyDamage(_damage);
                EventBus.Publish(new DamageDealtEvent
                {
                    Target = other.gameObject,
                    Amount = _damage
                });
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            OnReturnToPool?.Invoke(this);
        }
    }
}
