using System.Collections.Generic;
using Fusion;
using UnityEngine;
using VoidRogues.Enemies;
using VoidRogues.Player;

namespace VoidRogues.Props
{
    /// <summary>
    /// Authoritative destructible-props simulation for VoidRogues.
    ///
    /// Design (see Architecture.md §4 for full details):
    ///   - All state lives in a <see cref="NetworkArray{T}"/> of <see cref="PropState"/>
    ///     structs.  No per-prop <see cref="NetworkObject"/>.
    ///   - Explosion damage (chain reactions included) is resolved host-side using
    ///     <c>Physics2D.OverlapCircleAll</c>.
    ///   - Clients detect the <c>Exploding</c> flag change via <see cref="ChangeDetector"/>
    ///     and play the VFX locally.
    ///
    /// Attach to the ManagerHub NetworkObject in the Mission scene.
    /// </summary>
    public class PropsManager : NetworkBehaviour
    {
        public const int MaxProps = 256;

        [Header("Prop Database")]
        [Tooltip("Index must match PropState.TypeIndex.")]
        [SerializeField] private PropDefinition[] _propDatabase;

        [Header("Layers")]
        [SerializeField] private LayerMask _explosionLayers; // Enemy + Player

        // ------------------------------------------------------------------
        // Networked state
        // ------------------------------------------------------------------

        [Networked, Capacity(MaxProps)]
        private NetworkArray<PropState> _props { get; }

        // ------------------------------------------------------------------
        // Local state (non-networked)
        // ------------------------------------------------------------------

        private ChangeDetector _changes;
        private GameObject[]   _visuals;

        // Collider-to-index lookup rebuilt each render frame.
        private readonly Dictionary<Collider2D, int> _colliderIndex = new Dictionary<Collider2D, int>();
        private Collider2D[] _propColliders;

        // References for explosion damage routing.
        private EnemyManager    _enemyManager;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _changes       = GetChangeDetector(ChangeDetector.Source.SimulationState);
            _visuals        = new GameObject[MaxProps];
            _propColliders  = new Collider2D[MaxProps];
            _enemyManager  = FindObjectOfType<EnemyManager>();
        }

        // ------------------------------------------------------------------
        // Simulation (host only)
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsServer) return;

            for (int i = 0; i < MaxProps; i++)
            {
                var state = _props[i];
                if (!state.IsActive) continue;

                // Deactivate a prop that already exploded last tick so clients have
                // at least one tick to see the Exploding flag before it disappears.
                if (state.Exploding)
                {
                    state.IsActive  = false;
                    state.Exploding = false;
                    _props.Set(i, state);
                }
            }
        }

        // ------------------------------------------------------------------
        // Presentation (all peers)
        // ------------------------------------------------------------------

        public override void Render()
        {
            _colliderIndex.Clear();

            foreach (var change in _changes.DetectChanges(this))
            {
                if (change == nameof(_props))
                {
                    RefreshAllVisuals();
                    return;
                }
            }
        }

        private void RefreshAllVisuals()
        {
            for (int i = 0; i < MaxProps; i++)
            {
                var state = _props[i];

                if (!state.IsActive)
                {
                    if (_visuals[i] != null) _visuals[i].SetActive(false);
                    continue;
                }

                EnsureVisual(i, state.TypeIndex);
                var go = _visuals[i];
                if (go == null) continue;

                go.SetActive(true);
                go.transform.position = state.Position;

                // Register collider for hit lookups.
                var col = _propColliders[i];
                if (col != null) _colliderIndex[col] = i;

                // Play explosion VFX if flag just flipped.
                if (state.Exploding)
                {
                    PlayExplosionVFX(i, state);
                    go.SetActive(false);
                }
            }
        }

        private void PlayExplosionVFX(int index, PropState state)
        {
            if (state.TypeIndex >= _propDatabase.Length) return;
            var def = _propDatabase[state.TypeIndex];
            if (def.ExplosionVFXPrefab == null) return;

            var vfx = Instantiate(def.ExplosionVFXPrefab, state.Position, Quaternion.identity);
            vfx.Play();
            Destroy(vfx.gameObject, vfx.main.duration + 0.5f);
        }

        private void EnsureVisual(int index, byte typeIndex)
        {
            if (_visuals[index] != null) return;
            if (typeIndex >= _propDatabase.Length) return;

            var def = _propDatabase[typeIndex];
            if (def.VisualPrefab == null) return;

            var go = Instantiate(def.VisualPrefab);
            _visuals[index]    = go;
            _propColliders[index] = go.GetComponent<Collider2D>();
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Places a new prop at the given world position.  Host only.
        /// </summary>
        public void RegisterProp(byte typeIndex, Vector2 position)
        {
            if (!Runner.IsServer) return;
            if (typeIndex >= _propDatabase.Length) return;

            for (int i = 0; i < MaxProps; i++)
            {
                if (_props[i].IsActive) continue;

                _props.Set(i, new PropState
                {
                    IsActive  = true,
                    Position  = position,
                    TypeIndex = typeIndex,
                    Health    = _propDatabase[typeIndex].MaxHealth,
                    Exploding = false,
                });
                return;
            }

            Debug.LogWarning("[PropsManager] All prop slots are occupied.");
        }

        /// <summary>
        /// Applies damage to a prop.  Host only.
        /// Chain explosions are triggered automatically if the prop is explosive.
        /// </summary>
        public void DamageProp(int index, int amount)
        {
            if (!Runner.IsServer) return;
            if (index < 0 || index >= MaxProps) return;

            var state = _props[index];
            if (!state.IsActive) return;

            state.Health = (short)Mathf.Max(0, state.Health - amount);

            if (state.Health <= 0 && !state.Exploding)
            {
                state.Exploding = true;
                _props.Set(index, state);
                TriggerExplosion(index, state);
            }
            else
            {
                _props.Set(index, state);
            }
        }

        private void TriggerExplosion(int index, PropState state)
        {
            if (state.TypeIndex >= _propDatabase.Length) return;
            var def = _propDatabase[state.TypeIndex];
            if (!def.IsExplosive) return;

            var hits = Physics2D.OverlapCircleAll(state.Position, def.ExplosionRadius, _explosionLayers);

            foreach (var hit in hits)
            {
                // Damage enemies.
                if (_enemyManager != null)
                {
                    int enemyIdx = _enemyManager.GetEnemyIndexForCollider(hit);
                    if (enemyIdx >= 0)
                    {
                        _enemyManager.DamageEnemy(enemyIdx, def.ExplosionDamage);
                        continue;
                    }
                }

                // Damage players.
                var player = hit.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(def.ExplosionDamage);
                    continue;
                }

                // Chain explosion on other props.
                int propIdx = GetPropIndexForCollider(hit);
                if (propIdx >= 0 && propIdx != index)
                {
                    DamageProp(propIdx, def.ExplosionDamage);
                }
            }
        }

        /// <summary>Returns the array index for a prop that owns the given collider, or -1.</summary>
        public int GetPropIndexForCollider(Collider2D col)
        {
            return _colliderIndex.TryGetValue(col, out int idx) ? idx : -1;
        }
    }
}
