using Fusion;
using UnityEngine;

namespace VoidRogues.Enemies
{
    /// <summary>
    /// Blittable struct for a single enemy's networked state.
    ///
    /// Stored in <see cref="EnemyManager._enemies"/> (a <see cref="NetworkArray{T}"/>).
    /// The host writes this struct every tick; clients read it via <see cref="ChangeDetector"/>.
    /// </summary>
    public struct EnemyState : INetworkStruct
    {
        /// <summary>True while this slot has a live enemy.</summary>
        public NetworkBool IsActive;

        /// <summary>World-space position.</summary>
        public Vector2 Position;

        /// <summary>Current velocity (units/second).</summary>
        public Vector2 Velocity;

        /// <summary>Index into <see cref="EnemyManager._enemyDatabase"/>.</summary>
        public byte TypeIndex;

        /// <summary>Current health points.</summary>
        public short Health;

        /// <summary>
        /// Animation state: 0=Idle, 1=Walk, 2=Attack, 3=Death.
        /// Clients drive the <c>Animator</c> from this value.
        /// </summary>
        public byte AnimState;

        /// <summary>
        /// <see cref="PlayerRef.AsIndex"/> of the current chase target.
        /// -1 means no target.
        /// </summary>
        public int TargetPlayer;
    }
}
