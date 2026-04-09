using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoidRogues
{
    [StructLayout(LayoutKind.Explicit, Size = 7)]
    public struct FWorldPosition : INetworkStruct
    {
        [FieldOffset(0)] private byte _gridXZ;               // 1 byte: 4 bits for GridX, 4 bits for GridZ
        [FieldOffset(1)] private ushort _localX;             // 2 bytes: 0–650.00 scaled by 100
        [FieldOffset(3)] private short _localYCompressed;    // 2 bytes: -327.68 to 327.67 scaled by 100
        [FieldOffset(5)] private ushort _localZ;             // 2 bytes: 0–650.00 scaled by 100

        private const float LOCAL_SCALE = 100f;     // 0.01 precision
        private const float LOCAL_MAX = 650.0f;     // Max distance inside a grid cell

        private const float Y_MIN = -327.68f;
        private const float Y_MAX = 327.67f;

        // Grid Accessors (0–15 for both)
        public int GridX
        {
            get => (_gridXZ >> 4) & 0x0F;
            set => _gridXZ = (byte)((_gridXZ & 0x0F) | ((value & 0x0F) << 4));
        }

        public int GridZ
        {
            get => _gridXZ & 0x0F;
            set => _gridXZ = (byte)((_gridXZ & 0xF0) | (value & 0x0F));
        }

        public Vector2Int GridPosition => new Vector2Int(GridX, GridZ);

        // Local Position Accessors (within grid cell, 0.00–650.00)
        public float LocalX
        {
            get => _localX / LOCAL_SCALE;
            set => _localX = (ushort)(Mathf.Clamp(value, 0, LOCAL_MAX) * LOCAL_SCALE);
        }

        public float LocalY
        {
            get => _localYCompressed / LOCAL_SCALE;
            set => _localYCompressed = (short)(Mathf.Clamp(value, Y_MIN, Y_MAX) * LOCAL_SCALE);
        }

        public float LocalZ
        {
            get => _localZ / LOCAL_SCALE;
            set => _localZ = (ushort)(Mathf.Clamp(value, 0, LOCAL_MAX) * LOCAL_SCALE);
        }

        public Vector3 LocalPosition
        {
            get => new Vector3(LocalX, LocalY, LocalZ);
            set
            {
                LocalX = value.x;
                LocalY = value.y;
                LocalZ = value.z;
            }
        }

        // World Coordinate Accessors
        public float PositionX
        {
            get => GridX * LOCAL_MAX + LocalX;
            set
            {
                GridX = Mathf.Clamp((int)(value / LOCAL_MAX), 0, 15);
                LocalX = value - GridX * LOCAL_MAX;
            }
        }

        public float PositionY
        {
            get => LocalY;
            set => LocalY = value;
        }

        public float PositionZ
        {
            get => GridZ * LOCAL_MAX + LocalZ;
            set
            {
                GridZ = Mathf.Clamp((int)(value / LOCAL_MAX), 0, 15);
                LocalZ = value - GridZ * LOCAL_MAX;
            }
        }

        public Vector3 Position
        {
            get => new Vector3(PositionX, PositionY, PositionZ);
            set
            {
                PositionX = value.x;
                PositionY = value.y;
                PositionZ = value.z;
            }
        }

        // Copy from world-space Vector3
        public void CopyPosition(Vector3 worldPosition)
        {
            Position = worldPosition;
        }

        // Copy from another FWorldPosition
        public void CopyPosition(in FWorldPosition other)
        {
            _gridXZ = other._gridXZ;
            _localX = other._localX;
            _localYCompressed = other._localYCompressed;
            _localZ = other._localZ;
        }

        // Equality check
        public bool IsPositionEqual(ref FWorldPosition other)
        {
            return _gridXZ == other._gridXZ &&
                   _localX == other._localX &&
                   _localYCompressed == other._localYCompressed &&
                   _localZ == other._localZ;
        }

        // Debug helper
        public string DebugString()
        {
            return $"Grid: ({GridX}, {GridZ}), " +
                   $"Local: ({LocalX:F2}, {LocalY:F2}, {LocalZ:F2}), " +
                   $"World: ({PositionX:F2}, {PositionY:F2}, {PositionZ:F2})";
        }
    }
}
