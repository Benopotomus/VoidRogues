using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// Authoritative enemy simulation for VoidRogues.
    ///
    /// Design (see Architecture.md §3 for full details):
    ///   - A single <see cref="NetworkBehaviour"/> owns all enemies in the scene.
    ///   - State lives in a fixed-size <see cref="NetworkArray{T}"/> of
    ///     <see cref="EnemyState"/> structs (up to <see cref="MaxEnemies"/>).
    ///   - AI logic runs on the host only inside <see cref="FixedUpdateNetwork"/>.
    ///   - Clients receive state deltas and update visual GameObjects in
    ///     <see cref="Render"/>.
    ///
    /// Attach to the ManagerHub NetworkObject in the Mission scene.
    /// </summary>
    public class EnemyManager : NetworkBehaviour
    {
        public const int MaxEnemies = 512;

        [Header("Enemy Database")]
        [Tooltip("Index must match EnemyState.TypeIndex.")]
        [SerializeField] private EnemyDefinition[] _enemyDatabase;

        [Header("AI Settings")]
        [SerializeField] private float _separationRadius = 0.5f;
        [SerializeField] private float _separationForce  = 2f;

        // ------------------------------------------------------------------
        // Networked state
        // ------------------------------------------------------------------

        [Networked, Capacity(MaxEnemies)]
        private NetworkArray<EnemyState> _enemies { get; }

        // ------------------------------------------------------------------
        // Local state (non-networked)
        // ------------------------------------------------------------------

        private ChangeDetector _changes;

        private GameObject[]  _visuals;
        private Animator[]    _animators;

        // Player positions cached per-tick for AI (host only).
        private readonly List<Vector2> _playerPositions = new List<Vector2>(4);

        // Collider-to-index lookup populated each render frame.
        private readonly Dictionary<Collider2D, int> _colliderIndex = new Dictionary<Collider2D, int>();

        // Colliders used for overlap checks on each enemy visual (local only).
        private Collider2D[] _enemyColliders;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _changes = GetChangeDetector(ChangeDetector.Source.SimulationState);

            _visuals        = new GameObject[MaxEnemies];
            _animators      = new Animator[MaxEnemies];
            _enemyColliders = new Collider2D[MaxEnemies];
        }

        // ------------------------------------------------------------------
        // Simulation (host only meaningful path)
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!Runner.IsServer) return;

            CachePlayerPositions();

            for (int i = 0; i < MaxEnemies; i++)
            {
                var state = _enemies[i];
                if (!state.IsActive) continue;

                state = EnemyAI.Tick(state, _playerPositions, _enemyDatabase, Runner.DeltaTime,
                                     _separationRadius, _separationForce, i, _enemies);
                _enemies.Set(i, state);
            }
        }

        private void CachePlayerPositions()
        {
            _playerPositions.Clear();
            foreach (var player in Runner.ActivePlayers)
            {
                if (Runner.TryGetPlayerObject(player, out var obj))
                {
                    _playerPositions.Add(obj.transform.position);
                }
            }
        }

        // ------------------------------------------------------------------
        // Presentation (all peers)
        // ------------------------------------------------------------------

        public override void Render()
        {
            _colliderIndex.Clear();

            for (int i = 0; i < MaxEnemies; i++)
            {
                var state = _enemies[i];

                if (state.IsActive)
                {
                    EnsureVisual(i, state.TypeIndex);
                    var go = _visuals[i];
                    go.SetActive(true);
                    go.transform.position = state.Position;

                    // Update animator state.
                    if (_animators[i] != null)
                    {
                        _animators[i].SetInteger("State", state.AnimState);
                    }

                    // Register collider for hit lookup.
                    var col = _enemyColliders[i];
                    if (col != null)
                    {
                        _colliderIndex[col] = i;
                    }
                }
                else if (_visuals[i] != null)
                {
                    _visuals[i].SetActive(false);
                }
            }
        }

        private void EnsureVisual(int index, byte typeIndex)
        {
            if (_visuals[index] != null) return;
            if (typeIndex >= _enemyDatabase.Length) return;

            var def = _enemyDatabase[typeIndex];
            if (def.VisualPrefab == null) return;

            var go = Instantiate(def.VisualPrefab);
            _visuals[index] = go;

            _animators[index] = go.GetComponent<Animator>();
            if (_animators[index] != null && def.AnimatorController != null)
            {
                _animators[index].runtimeAnimatorController = def.AnimatorController;
            }

            _enemyColliders[index] = go.GetComponent<Collider2D>();
        }

        // ------------------------------------------------------------------
        // Public API
        // ------------------------------------------------------------------

        /// <summary>
        /// Activates an enemy slot.  Must be called on the host.
        /// </summary>
        public void ActivateEnemy(byte typeIndex, Vector2 position)
        {
            if (!Runner.IsServer) return;
            if (typeIndex >= _enemyDatabase.Length) return;

            for (int i = 0; i < MaxEnemies; i++)
            {
                if (_enemies[i].IsActive) continue;

                _enemies.Set(i, new EnemyState
                {
                    IsActive     = true,
                    Position     = position,
                    TypeIndex    = typeIndex,
                    Health       = _enemyDatabase[typeIndex].MaxHealth,
                    AnimState    = 1, // Walk
                    TargetPlayer = -1,
                });
                return;
            }

            Debug.LogWarning("[EnemyManager] All enemy slots are occupied.");
        }

        /// <summary>
        /// Applies damage to an enemy.  Must be called on the host.
        /// </summary>
        public void DamageEnemy(int index, int amount)
        {
            if (!Runner.IsServer) return;
            if (index < 0 || index >= MaxEnemies) return;

            var state = _enemies[index];
            if (!state.IsActive) return;

            state.Health = (short)Mathf.Max(0, state.Health - amount);

            if (state.Health <= 0)
            {
                state.AnimState = 3; // Death
                state.IsActive  = false;
            }

            _enemies.Set(index, state);
        }

        /// <summary>
        /// Returns the array index for an enemy that owns the given collider, or -1.
        /// </summary>
        public int GetEnemyIndexForCollider(Collider2D col)
        {
            return _colliderIndex.TryGetValue(col, out int idx) ? idx : -1;
        }

        /// <summary>Number of currently active enemies.</summary>
        public int ActiveEnemyCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < MaxEnemies; i++)
                {
                    if (_enemies[i].IsActive) count++;
                }
                return count;
            }
        }
    }
}
