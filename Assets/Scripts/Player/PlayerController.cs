using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Player
{
    /// <summary>
    /// Handles player movement, dodge-roll, and animation state transitions.
    /// Reads input from <see cref="PlayerInputReader"/> and applies movement
    /// via Rigidbody2D in FixedUpdate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInputReader))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Dodge Roll")]
        [SerializeField] private float dodgeForce       = 18f;
        [SerializeField] private float dodgeDuration    = 0.2f;
        [SerializeField] private float dodgeCooldown    = 0.6f;
        [SerializeField] private float invincibilityTime = 0.4f;

        private Rigidbody2D _rb;
        private PlayerInputReader _input;
        private HealthSystem _health;
        private Animator _animator;

        private Vector2 _moveDir;
        private Vector2 _lastMoveDir = Vector2.down;

        private bool _isDodging;
        private float _dodgeTimer;
        private float _dodgeCooldownTimer;

        private static readonly int AnimMoveX    = Animator.StringToHash("MoveX");
        private static readonly int AnimMoveY    = Animator.StringToHash("MoveY");
        private static readonly int AnimMoving   = Animator.StringToHash("IsMoving");
        private static readonly int AnimDodging  = Animator.StringToHash("IsDodging");

        private void Awake()
        {
            _rb     = GetComponent<Rigidbody2D>();
            _input  = GetComponent<PlayerInputReader>();
            _health = GetComponent<HealthSystem>();
            _animator = GetComponent<Animator>();

            // Register this as the cached player transform so enemies can find it cheaply.
            if (GameManager.Instance != null)
                GameManager.Instance.PlayerTransform = transform;
        }

        private void OnEnable()
        {
            _input.OnDodge += TryDodge;
        }

        private void OnDisable()
        {
            _input.OnDodge -= TryDodge;
        }

        private void Update()
        {
            _moveDir = _input.MoveInput;
            if (_moveDir.sqrMagnitude > 0.01f)
                _lastMoveDir = _moveDir.normalized;

            UpdateDodgeTimers();
            UpdateAnimator();
        }

        private void FixedUpdate()
        {
            if (_isDodging)
                return;

            float speed = GameManager.Instance != null
                ? GameManager.Instance.Run.MoveSpeed
                : moveSpeed;

            _rb.linearVelocity = _moveDir * speed;
        }

        // ── Dodge roll ────────────────────────────────────────────────────────

        private void TryDodge()
        {
            if (_isDodging || _dodgeCooldownTimer > 0f)
                return;

            _isDodging        = true;
            _dodgeTimer       = dodgeDuration;
            _dodgeCooldownTimer = dodgeCooldown;

            _rb.linearVelocity = _lastMoveDir * dodgeForce;

            if (_health != null)
                _health.SetInvincible(invincibilityTime);
        }

        private void UpdateDodgeTimers()
        {
            if (_dodgeCooldownTimer > 0f)
                _dodgeCooldownTimer -= Time.deltaTime;

            if (_isDodging)
            {
                _dodgeTimer -= Time.deltaTime;
                if (_dodgeTimer <= 0f)
                    _isDodging = false;
            }
        }

        // ── Animator ──────────────────────────────────────────────────────────

        private void UpdateAnimator()
        {
            if (_animator == null)
                return;

            _animator.SetFloat(AnimMoveX, _lastMoveDir.x);
            _animator.SetFloat(AnimMoveY, _lastMoveDir.y);
            _animator.SetBool(AnimMoving,  _moveDir.sqrMagnitude > 0.01f);
            _animator.SetBool(AnimDodging, _isDodging);
        }
    }
}
