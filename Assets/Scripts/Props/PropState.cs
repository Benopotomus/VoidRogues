using Fusion;
using UnityEngine;

namespace VoidRogues.Props
{
    /// <summary>
    /// Blittable struct for a single destructible prop's networked state.
    ///
    /// Stored in <see cref="PropsManager._props"/> (a <see cref="NetworkArray{T}"/>).
    /// </summary>
    public struct PropState : INetworkStruct
    {
        /// <summary>True while this slot contains a living or exploding prop.</summary>
        public NetworkBool IsActive;

        /// <summary>World-space position (props are static after spawn).</summary>
        public Vector2 Position;

        /// <summary>Index into <see cref="PropsManager._propDatabase"/>.</summary>
        public byte TypeIndex;

        /// <summary>Current health points.</summary>
        public short Health;

        /// <summary>
        /// Set to <c>true</c> by the host when health reaches zero.
        /// Clients detect this change and play the explosion effect.
        /// </summary>
        public NetworkBool Exploding;
    }
}
