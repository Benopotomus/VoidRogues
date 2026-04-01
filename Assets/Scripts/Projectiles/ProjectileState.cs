using Fusion;
using UnityEngine;

namespace VoidRogues.Projectiles
{
    /// <summary>
    /// Blittable struct stored in <see cref="ProjectileManager._projectiles"/> array.
    ///
    /// Rules:
    ///   - No managed references (strings, classes, arrays).
    ///   - Use <see cref="NetworkBool"/> instead of <c>bool</c>.
    ///   - Keep under 32 bytes where possible for Fusion's efficient serialisation.
    /// </summary>
    public struct ProjectileState : INetworkStruct
    {
        /// <summary>Slot is in use and projectile is flying.</summary>
        public NetworkBool IsActive;

        /// <summary>World-space position updated every tick.</summary>
        public Vector2 Position;

        /// <summary>Velocity (units/second) in world space.</summary>
        public Vector2 Velocity;

        /// <summary>Player index that fired the projectile (maps to <see cref="PlayerRef.AsIndex"/>).</summary>
        public byte OwnerId;

        /// <summary>Maps to a <see cref="VoidRogues.Player.WeaponDefinition"/> in the weapon database.</summary>
        public byte WeaponTypeIndex;

        /// <summary>Tick on which the projectile was spawned (for lifetime checks).</summary>
        public int SpawnTick;

        /// <summary>Set by the host when the projectile hits something this tick.</summary>
        public NetworkBool DidHit;
    }
}
