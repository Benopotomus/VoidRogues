using Fusion;
using UnityEngine;
using VoidRogues.Network;
using VoidRogues.Projectiles;

namespace VoidRogues.Player
{
    /// <summary>
    /// Top-down 2D character controller driven by Fusion <see cref="NetworkInputData"/>.
    ///
    /// Movement:
    ///   - Keyboard WASD (or left stick) drives <see cref="Rigidbody2D.MovePosition"/>.
    ///   - Uses Kinematic Rigidbody2D so Fusion can reconcile positions across clients.
    ///
    /// Requires:
    ///   - <see cref="NetworkRigidbody2D"/> on the same GameObject.
    ///   - <see cref="CapsuleCollider2D"/> (horizontal, at feet) on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(NetworkRigidbody2D))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private Transform      _gunPivot;

        // Cached components
        private NetworkRigidbody2D _networkRb;
        private Rigidbody2D        _rb;
        private PlayerShooter      _shooter;

        // Networked state
        [Networked] private PlayerNetworkData State { get; set; }

        private ChangeDetector _changes;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _networkRb = GetComponent<NetworkRigidbody2D>();
            _rb        = GetComponent<Rigidbody2D>();
            _shooter   = GetComponent<PlayerShooter>();
            _changes   = GetChangeDetector(ChangeDetector.Source.SimulationState);

            // Initialise networked state on the host.
            if (Object.HasStateAuthority)
            {
                State = new PlayerNetworkData { Health = 100, IsAlive = true };
            }
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!State.IsAlive) return;

            if (Runner.TryGetInputForPlayer<NetworkInputData>(Object.InputAuthority, out var input))
            {
                ApplyMovement(input.Move);
                UpdateAimAngle(input.AimWorldPos);
            }
        }

        private void ApplyMovement(Vector2 moveDir)
        {
            var desiredVelocity = moveDir.normalized * _moveSpeed;
            _rb.MovePosition(_rb.position + desiredVelocity * Runner.DeltaTime);

            // Flip sprite to face movement direction.
            if (moveDir.x != 0 && _spriteRenderer != null)
            {
                _spriteRenderer.flipX = moveDir.x < 0;
            }
        }

        private void UpdateAimAngle(Vector2 aimWorldPos)
        {
            // Derive direction from player position to mouse world pos.
            var dir = (Vector3)aimWorldPos - transform.position;

            if (dir.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                var s = State;
                s.AimAngle = angle;
                State = s;
            }
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        public override void Render()
        {
            // Rotate gun pivot to face the authoritative aim angle.
            if (_gunPivot != null)
            {
                _gunPivot.rotation = Quaternion.Euler(0, 0, State.AimAngle);
            }
        }

        // ------------------------------------------------------------------
        // Damage (called by EnemyManager / PropsManager on the host)
        // ------------------------------------------------------------------

        public void TakeDamage(int amount)
        {
            if (!Object.HasStateAuthority || !State.IsAlive) return;

            var s = State;
            s.Health = (short)Mathf.Max(0, s.Health - amount);
            s.IsAlive = s.Health > 0;
            State = s;

            if (!State.IsAlive)
            {
                OnDeath();
            }
        }

        private void OnDeath()
        {
            // TODO: trigger death animation, respawn logic.
            Debug.Log($"[PlayerController] Player {Object.InputAuthority} died.");
        }

        // ------------------------------------------------------------------
        // Public accessors
        // ------------------------------------------------------------------

        public bool    IsAlive   => State.IsAlive;
        public int     Health    => State.Health;
        public float   AimAngle  => State.AimAngle;
    }
}
