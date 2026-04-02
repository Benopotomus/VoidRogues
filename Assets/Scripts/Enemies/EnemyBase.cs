using UnityEngine;
using VoidRogues.Combat;
using VoidRogues.Core;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// Base class for all enemies. Handles health, damage reception, death,
    /// and loot dropping. Concrete AI behaviour is implemented in subclasses.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public abstract class EnemyBase : MonoBehaviour, IDamageable
    {
        [SerializeField] protected EnemyDataSO data;
        [SerializeField] private GameObject fragmentPickupPrefab;
        [SerializeField] private GameObject itemPickupPrefab;

        protected int CurrentHealth;
        protected Transform PlayerTransform;

        // State machine
        protected enum State { Idle, Chase, Attack, Dead }
        protected State CurrentState = State.Idle;

        public bool IsAlive => CurrentState != State.Dead;

        // ── Unity lifecycle ───────────────────────────────────────────────────

        protected virtual void Awake()
        {
            int sector = GameManager.Instance != null
                ? GameManager.Instance.Run.CurrentSector
                : 1;
            CurrentHealth = ScaleHealth(data.maxHealth, sector);
        }

        protected virtual void Start()
        {
            if (GameManager.Instance != null)
                PlayerTransform = GameManager.Instance.PlayerTransform;

            // Fallback for editor testing without GameManager
            if (PlayerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null)
                    PlayerTransform = player.transform;
            }
        }

        protected virtual void Update()
        {
            if (!IsAlive)
                return;

            UpdateStateMachine();
        }

        // ── IDamageable ───────────────────────────────────────────────────────

        /// <summary>Apply damage and transition to Dead when health reaches zero.</summary>
        public virtual void ApplyDamage(int amount)
        {
            if (!IsAlive)
                return;

            CurrentHealth -= amount;

            EventBus.Publish(new DamageDealtEvent
            {
                Target = gameObject,
                Amount = amount
            });

            if (CurrentHealth <= 0)
                Die();
        }

        // ── State machine (override in subclasses) ────────────────────────────

        protected virtual void UpdateStateMachine()
        {
            switch (CurrentState)
            {
                case State.Idle:   OnIdle();   break;
                case State.Chase:  OnChase();  break;
                case State.Attack: OnAttack(); break;
            }
        }

        protected virtual void OnIdle()   { }
        protected virtual void OnChase()  { }
        protected virtual void OnAttack() { }

        // ── Death ─────────────────────────────────────────────────────────────

        protected virtual void Die()
        {
            CurrentState = State.Dead;
            DropLoot();

            EventBus.Publish(new EnemyKilledEvent
            {
                Enemy    = gameObject,
                Position = transform.position
            });

            if (GameManager.Instance != null)
                GameManager.Instance.Run.EnemiesKilled++;

            Destroy(gameObject, 0.1f);
        }

        // ── Loot ──────────────────────────────────────────────────────────────

        private void DropLoot()
        {
            // Fragment drops
            int fragCount = Random.Range(data.fragmentDropMin, data.fragmentDropMax + 1);
            if (fragmentPickupPrefab != null && fragCount > 0)
                Instantiate(fragmentPickupPrefab, transform.position, Quaternion.identity);

            // Item drop (chance-based)
            if (itemPickupPrefab != null && Random.value < data.itemDropChance)
                Instantiate(itemPickupPrefab, transform.position, Quaternion.identity);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private int ScaleHealth(int baseHealth, int sector)
        {
            float scale = Mathf.Pow(data.healthScalePerSector, sector - 1);
            return Mathf.RoundToInt(baseHealth * scale);
        }

        protected int ScaledDamage(int baseDamage)
        {
            int sector = GameManager.Instance != null
                ? GameManager.Instance.Run.CurrentSector : 1;
            float scale = Mathf.Pow(data.damageScalePerSector, sector - 1);
            return Mathf.RoundToInt(baseDamage * scale);
        }
    }
}
