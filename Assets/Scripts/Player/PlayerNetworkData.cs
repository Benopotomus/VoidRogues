using Fusion;
using UnityEngine;

namespace VoidRogues.Player
{
    /// <summary>
    /// Per-player networked state stored as a blittable struct.
    /// Kept as a <c>[Networked]</c> property on <see cref="PlayerController"/> so
    /// Fusion serialises only changed fields each tick.
    /// </summary>
    public struct PlayerNetworkData : INetworkStruct
    {
        public short       Health;
        public NetworkBool IsAlive;
        public float       AimAngle;    // degrees, world space
        public byte        WeaponSlot;  // currently equipped weapon index
        public short       Score;
    }
}
