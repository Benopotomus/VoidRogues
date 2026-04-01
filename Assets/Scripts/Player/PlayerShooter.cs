using Fusion;
using UnityEngine;
using VoidRogues.Network;
using VoidRogues.Projectiles;

namespace VoidRogues.Player
{
    /// <summary>
    /// Handles twin-stick shooting for the player character.
    ///
    /// Keyboard/gamepad moves the character; mouse (or right stick) aims.
    /// Firing is authoritative on the host: this script converts fire input into a
    /// <see cref="ProjectileManager.SpawnProjectile"/> call so all projectiles are
    /// tracked in the shared <see cref="ProjectileState"/> array.
    ///
    /// Requires <see cref="PlayerController"/> on the same GameObject.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerShooter : NetworkBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private WeaponDefinition _startingWeapon;
        [SerializeField] private Transform        _muzzlePoint;

        [Header("VFX")]
        [SerializeField] private ParticleSystem _muzzleFlash;

        // Reference injected by the scene's ProjectileManager at spawn time.
        private ProjectileManager _projectileManager;

        // Fire-rate timer (tick-based).
        [Networked] private int _nextFireTick { get; set; }

        private PlayerController _controller;

        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        public override void Spawned()
        {
            _controller       = GetComponent<PlayerController>();
            _projectileManager = Runner.GetComponent<ProjectileManager>();

            if (_projectileManager == null)
            {
                _projectileManager = Object.Runner.SimulationUnityScene
                    .GetComponent<ProjectileManager>();
            }
        }

        // ------------------------------------------------------------------
        // Simulation
        // ------------------------------------------------------------------

        public override void FixedUpdateNetwork()
        {
            if (!_controller.IsAlive) return;
            if (_projectileManager == null) return;

            if (!Runner.TryGetInputForPlayer<NetworkInputData>(Object.InputAuthority, out var input))
                return;

            bool wantFire = input.FireHeld;
            if (wantFire && Runner.Tick >= _nextFireTick)
            {
                Fire(input.AimWorldPos);
            }
        }

        private void Fire(Vector2 aimWorldPos)
        {
            var weapon = _startingWeapon;
            if (weapon == null) return;

            var origin    = (Vector2)(_muzzlePoint != null ? _muzzlePoint.position : transform.position);
            var aimDir    = (aimWorldPos - (Vector2)transform.position).normalized;

            // Handle spread for shotgun-style weapons.
            for (int i = 0; i < weapon.ProjectileCount; i++)
            {
                float spreadAngle = 0f;
                if (weapon.ProjectileCount > 1)
                {
                    spreadAngle = Random.Range(-weapon.SpreadDegrees * 0.5f, weapon.SpreadDegrees * 0.5f);
                }

                var dir = Rotate(aimDir, spreadAngle);
                _projectileManager.SpawnProjectile(
                    Object.InputAuthority.AsIndex,
                    origin,
                    dir * weapon.ProjectileSpeed,
                    weapon.WeaponTypeIndex);
            }

            // Advance fire cooldown (ticks).
            int ticksPerShot = Mathf.Max(1, Mathf.RoundToInt(Runner.Config.Simulation.TickRate / weapon.FireRate));
            _nextFireTick = Runner.Tick + ticksPerShot;
        }

        // ------------------------------------------------------------------
        // Presentation
        // ------------------------------------------------------------------

        public override void Render()
        {
            // Play muzzle flash on the local machine if we just fired.
            // (A simple heuristic: fire tick changed this render frame.)
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
