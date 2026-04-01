using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// Grunt AI: slowly chases the player and deals contact damage.
    /// No ranged attack. Transitions Chase ↔ Idle based on detection radius.
    /// </summary>
    public class EnemyAI_Grunt : EnemyBase
    {
        [SerializeField] private float detectionRadius = 8f;
        [SerializeField] private float contactDamageInterval = 0.8f;

        private Rigidbody2D _rb;
        private float _contactTimer;

        protected override void Awake()
        {
            base.Awake();
            _rb = GetComponent<Rigidbody2D>();
        }

        protected override void OnIdle()
        {
            if (PlayerTransform == null)
                return;

            float dist = Vector2.Distance(transform.position, PlayerTransform.position);
            if (dist <= detectionRadius)
                CurrentState = State.Chase;
        }

        protected override void OnChase()
        {
            if (PlayerTransform == null)
                return;

            Vector2 dir = ((Vector2)PlayerTransform.position - (Vector2)transform.position).normalized;
            _rb.linearVelocity = dir * data.moveSpeed;

            // Switch to Attack when overlapping the player
            float dist = Vector2.Distance(transform.position, PlayerTransform.position);
            if (dist < 0.6f)
                CurrentState = State.Attack;

            // Return to Idle if player moves out of range
            if (dist > detectionRadius * 1.2f)
            {
                _rb.linearVelocity = Vector2.zero;
                CurrentState = State.Idle;
            }
        }

        protected override void OnAttack()
        {
            _rb.linearVelocity = Vector2.zero;
            _contactTimer -= Time.deltaTime;

            if (_contactTimer <= 0f)
            {
                _contactTimer = contactDamageInterval;
                if (PlayerTransform != null)
                {
                    var damageable = PlayerTransform.GetComponent<Combat.IDamageable>();
                    damageable?.ApplyDamage(ScaledDamage(data.contactDamage));
                }
            }

            // Resume chase if player moves away
            float dist = PlayerTransform != null
                ? Vector2.Distance(transform.position, PlayerTransform.position)
                : float.MaxValue;

            if (dist > 0.8f)
                CurrentState = State.Chase;
        }
    }
}
