using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Player
{
    /// <summary>
    /// Manages Sanity (HP) for the player. Publishes <see cref="SanityChangedEvent"/>
    /// on every change and <see cref="PlayerDeathEvent"/> on death.
    /// Also shared as the IDamageable implementation the combat system targets.
    /// </summary>
    public class HealthSystem : MonoBehaviour, Combat.IDamageable
    {
        [SerializeField] private int maxSanity = 100;

        private int _currentSanity;
        private float _invincibleTimer;

        public int CurrentSanity => _currentSanity;
        public int MaxSanity => maxSanity;
        public bool IsAlive => _currentSanity > 0;

        private void Awake()
        {
            _currentSanity = maxSanity;
        }

        private void Update()
        {
            if (_invincibleTimer > 0f)
                _invincibleTimer -= Time.deltaTime;
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        /// <summary>Apply damage, respecting invincibility frames.</summary>
        public void ApplyDamage(int amount)
        {
            if (_invincibleTimer > 0f || !IsAlive)
                return;

            _currentSanity = Mathf.Max(0, _currentSanity - amount);

            EventBus.Publish(new SanityChangedEvent
            {
                Current = _currentSanity,
                Max     = maxSanity
            });

            if (_currentSanity == 0)
                Die();
        }

        /// <summary>Restore Sanity, clamped to MaxSanity.</summary>
        public void Heal(int amount)
        {
            if (!IsAlive)
                return;

            _currentSanity = Mathf.Min(maxSanity, _currentSanity + amount);

            EventBus.Publish(new SanityChangedEvent
            {
                Current = _currentSanity,
                Max     = maxSanity
            });
        }

        /// <summary>Grant temporary invincibility (used by dodge roll).</summary>
        public void SetInvincible(float duration)
        {
            _invincibleTimer = Mathf.Max(_invincibleTimer, duration);
        }

        /// <summary>Set a new MaxSanity and optionally scale current Sanity.</summary>
        public void SetMaxSanity(int newMax, bool adjustCurrent = true)
        {
            int delta = newMax - maxSanity;
            maxSanity = newMax;
            if (adjustCurrent)
                _currentSanity = Mathf.Clamp(_currentSanity + delta, 0, maxSanity);

            EventBus.Publish(new SanityChangedEvent
            {
                Current = _currentSanity,
                Max     = maxSanity
            });
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void Die()
        {
            EventBus.Publish(new PlayerDeathEvent { LastPosition = transform.position });
        }
    }
}
