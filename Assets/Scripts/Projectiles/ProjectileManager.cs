using System.Collections.Generic;
using Fusion;
using UnityEngine;
using VoidRogues.Enemies;
using VoidRogues.Player;
using VoidRogues.Props;

namespace VoidRogues.Projectiles
{
    /// <summary>
    /// Authoritative projectile simulation for VoidRogues.
    ///
    /// Design:
    ///   - All state lives in a fixed-size <see cref="NetworkArray{T}"/> of
    ///     <see cref="ProjectileState"/> structs (capacity: <see cref="MaxProjectiles"/>).
    ///   - No per-projectile <see cref="NetworkObject"/>; visual GameObjects are managed
    ///     locally by a simple pool and driven by the networked array.
    ///   - Hit detection runs on the host using <c>Physics2D</c> circle casts.
    ///
    /// Attach this component to the ManagerHub NetworkObject in the Mission scene.
    /// </summary>
    public class ProjectileManager : NetworkBehaviour
    {
        public const int MaxProjectiles = 256;

        [Header("Weapon Database")]
        [Tooltip("Index in this array must match WeaponDefinition.WeaponTypeIndex.")]
        [SerializeField] private WeaponDefinition[] _weaponDatabase;

        [Header("Visual Pool")]
        [SerializeField] private GameObject _projectileVisualPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask _hitLayers; // Enemy + Props + Level

        // ------------------------------------------------------------------
        // Networked state
        // ------------------------------------------------------------------

        [Networked, Capacity(MaxProjectiles)]
        private NetworkArray<ProjectileState> _projectiles { get; }

        // ------------------------------------------------------------------
        // Local (non-networked) state
        // ------------------------------------------------------------------

        private ChangeDetector          _changes;
        private GameObject[]            _visualPool;
        private SpriteRenderer[]        _visualRenderers;
        /// <summary>Fallback lifetime used when a WeaponDefinition does not specify one.</summary>
        private const int DefaultMaxLifetimeTicks = 300; // ~4.7 s at 64 Hz

        // References injected at runtime
        private EnemyManager  _enemyManager;
        private PropsManager  _propsManager;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            _visualPool      = new GameObject[MaxProjectiles];
            _visualRenderers = new SpriteRenderer[MaxProjectiles];

            for (int i = 0; i < MaxProjectiles; i++)
            {
                var go = Instantiate(_projectileVisualPrefab);
                go.SetActive(false);
                _visualPool[i]      = go;
                _visualRenderers[i] = go.GetComponent<SpriteRenderer>();
            }

            _enemyManager = FindObjectOfType<EnemyManager>();
            _propsManager = FindObjectOfType<PropsManager>();
        }

        // ------------------------------------------------------------------
        // Simulation (runs on all peers)
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            bool isHost = Runner.IsServer;

            for (int i = 0; i < MaxProjectiles; i++)
            {
                var state = _projectiles[i];
                if (!state.IsActive) continue;

                // Advance position.
                state.Position += state.Velocity * Runner.DeltaTime;

                // Lifetime check (per-weapon or global default).
                int maxTicks = GetMaxLifetimeTicks(state.WeaponTypeIndex);
                if (Runner.Tick - state.SpawnTick > maxTicks)
                {
                    state.IsActive = false;
                    _projectiles.Set(i, state);
                    continue;
                }

                // Hit detection – host only.
                if (isHost)
                {
                    float radius = GetProjectileRadius(state.WeaponTypeIndex);
                    var hits = Physics2D.OverlapCircleAll(state.Position, radius, _hitLayers);

                    if (hits.Length > 0)
                    {
                        ProcessHit(i, ref state, hits);
                    }
                }

                _projectiles.Set(i, state);
            }
        }

        private void ProcessHit(int index, ref ProjectileState state, Collider2D[] hits)
        {
            int damage = GetDamage(state.WeaponTypeIndex);

            foreach (var hit in hits)
            {
                // Damage enemies.
                if (_enemyManager != null)
                {
                    int enemyIdx = _enemyManager.GetEnemyIndexForCollider(hit);
                    if (enemyIdx >= 0)
                    {
                        _enemyManager.DamageEnemy(enemyIdx, damage);
                        state.DidHit  = true;
                        state.IsActive = false;
                        return;
                    }
                }

                // Damage props.
                if (_propsManager != null)
                {
                    int propIdx = _propsManager.GetPropIndexForCollider(hit);
                    if (propIdx >= 0)
                    {
                        _propsManager.DamageProp(propIdx, damage);
                        state.DidHit  = true;
                        state.IsActive = false;
                        return;
                    }
                }

                // Damage players (friendly fire / enemies can shoot too).
                var player = hit.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(damage);
                    state.DidHit  = true;
                    state.IsActive = false;
                    return;
                }

                // Static level geometry.
                if (((1 << hit.gameObject.layer) & _hitLayers) != 0)
                {
                    state.DidHit  = true;
                    state.IsActive = false;
                    return;
                }
            }
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        public override void Render()
        {
            for (int i = 0; i < MaxProjectiles; i++)
            {
                var state = _projectiles[i];
                var go    = _visualPool[i];

                if (state.IsActive)
                {
                    go.SetActive(true);
                    go.transform.position = state.Position;

                    // Point sprite along velocity.
                    if (state.Velocity.sqrMagnitude > 0.01f)
                    {
                        float angle = Mathf.Atan2(state.Velocity.y, state.Velocity.x) * Mathf.Rad2Deg;
                        go.transform.rotation = Quaternion.Euler(0, 0, angle);
                    }

                    // Assign weapon-specific sprite.
                    var sprite = GetProjectileSprite(state.WeaponTypeIndex);
                    if (sprite != null && _visualRenderers[i] != null)
                    {
                        _visualRenderers[i].sprite = sprite;
                    }
                }
                else
                {
                    go.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------
        // Public API (called by PlayerShooter on the host)
        // ------------------------------------------------------------------

        /// <summary>
        /// Activates a projectile slot for a newly fired projectile.
        /// Must be called inside <see cref="NetworkBehaviour.FixedUpdateNetwork"/>.
        /// </summary>
        public void SpawnProjectile(int ownerId, Vector2 position, Vector2 velocity, byte weaponTypeIndex)
        {
            if (!Runner.IsServer) return;

            for (int i = 0; i < MaxProjectiles; i++)
            {
                if (_projectiles[i].IsActive) continue;

                _projectiles.Set(i, new ProjectileState
                {
                    IsActive       = true,
                    Position       = position,
                    Velocity       = velocity,
                    OwnerId        = (byte)ownerId,
                    WeaponTypeIndex = weaponTypeIndex,
                    SpawnTick      = Runner.Tick,
                    DidHit         = false,
                });
                return;
            }

            Debug.LogWarning("[ProjectileManager] All projectile slots are occupied.");
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private float GetProjectileRadius(byte weaponIndex)
        {
            if (weaponIndex < _weaponDatabase.Length)
                return _weaponDatabase[weaponIndex].ProjectileRadius;
            return 0.05f;
        }

        private int GetMaxLifetimeTicks(byte weaponIndex)
        {
            if (weaponIndex < _weaponDatabase.Length)
            {
                int perWeapon = _weaponDatabase[weaponIndex].MaxLifetimeTicks;
                return perWeapon > 0 ? perWeapon : DefaultMaxLifetimeTicks;
            }
            return DefaultMaxLifetimeTicks;
        }

        private int GetDamage(byte weaponIndex)
        {
            if (weaponIndex < _weaponDatabase.Length)
                return _weaponDatabase[weaponIndex].Damage;
            return 10;
        }

        private Sprite GetProjectileSprite(byte weaponIndex)
        {
            if (weaponIndex < _weaponDatabase.Length)
                return _weaponDatabase[weaponIndex].ProjectileSprite;
            return null;
        }
    }
}
