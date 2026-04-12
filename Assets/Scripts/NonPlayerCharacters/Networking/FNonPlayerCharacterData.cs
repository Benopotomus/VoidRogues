namespace VoidRogues.NonPlayerCharacters
{
    using Fusion;
    using UnityEngine;
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit, Size = 21)]
    public struct FNonPlayerCharacterData : INetworkStruct
    {
        [FieldOffset(0)]
        private int _configuration; // 4 bytes
        [FieldOffset(4)]
        private FWorldTransform _transform; // 9 bytes: Position (7) + Rotation (2)
        [FieldOffset(13)]
        private byte _condition; // 1 byte: NPCState (4 bits)// animation bits// Additive hit
        [FieldOffset(14)]
        private ushort _events; // 2 bytes: Health (12 bits) and storage

        // Total: 21 bytes

        public int DefinitionID
        {
            get => NonPlayerCharacterDataUtility.GetDefinitionID(ref this);
            set => NonPlayerCharacterDataUtility.SetDefinitionID(value, ref this);
        }

        public NonPlayerCharacterDefinition Definition
        {
            get => Global.Tables.NonPlayerCharacterTable.TryGetDefinition(DefinitionID);
        }

        public ENPCSpawnType SpawnType
        {
            get => NonPlayerCharacterDataUtility.GetSpawnType(ref this);
            set => NonPlayerCharacterDataUtility.SetSpawnType(value, ref this);
        }

        public NonPlayerCharacterDataDefinition DataDefinition
        {
            get => Definition.GetDataDefinition(SpawnType);
        }

        // TeamID
        public ETeamID TeamID
        {
            get => NonPlayerCharacterDataUtility.GetTeamID(ref this);
            set => NonPlayerCharacterDataUtility.SetTeamID(value, ref this);
        }

        public FWorldTransform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public Vector3 Position
        {
            get => _transform.Position;
            set => _transform.Position = value;
        }

        public float PositionX
        {
            get => _transform.PositionX;
            set => _transform.PositionX = value;
        }

        public float PositionY
        {
            get => _transform.PositionY;
            set => _transform.PositionY = value;
        }

        public float PositionZ
        {
            get => _transform.PositionZ;
            set => _transform.PositionZ = value;
        }

        public float Yaw
        {
            get => _transform.Yaw;
            set => _transform.Yaw = value;
        }

        public byte RawCompressedYaw
        {
            get => _transform.RawCompressedYaw;
            set => _transform.RawCompressedYaw = value;
        }

        public int TargetPlayerIndex
        {
            get
            {
                if(_transform.RawCompressedYaw > 240)
                    return _transform.RawCompressedYaw - 240;
                else
                    return -1;
            }
            set
            {
                _transform.RawCompressedYaw = (byte)(value + 240);
            }
        }

        public float Pitch
        {
            get => _transform.Pitch;
            set => _transform.Pitch = value;
        }

        public Quaternion Rotation
        {
            get => _transform.Rotation;
            set => _transform.Rotation = value;
        }

        public byte Condition
        {
            get => _condition;
            set => _condition = value;
        }

        public int Configuration
        {
            get => _configuration;
            set => _configuration = value;
        }

        public ushort Events
        {
            get => _events;
            set => _events = value;
        }



        public void Copy(FNonPlayerCharacterData other)
        {
            _transform = other._transform;
            _condition = other._condition;
            _configuration = other._configuration;
            _events = other._events;
        }

        public void Copy(ref FNonPlayerCharacterData other)
        {
            _transform = other._transform;
            _condition = other._condition;
            _configuration = other._configuration;
            _events = other._events;
        }

        public bool IsEqual(ref FNonPlayerCharacterData other)
        {
            return _condition == other._condition &&
                   _configuration == other._configuration &&
                   _events == other._events &&
                    _transform.IsEqual(ref other._transform);
        }

        public bool IsEqual(FNonPlayerCharacterData other)
        {
            return _condition == other._condition &&
                   _configuration == other._configuration &&
                   _events == other._events &&
                   _transform.IsEqual(ref other._transform);
        }

        public bool IsStateDataEqual(ref FNonPlayerCharacterData other)
        {
            return _condition == other._condition;
        }

        /// <summary>
        /// True when this NPC is actively moving toward its steering target.
        /// Stored in bit 18 of <c>_configuration</c>.
        /// </summary>
        public bool IsMoving
        {
            get => (_configuration & (1 << 18)) != 0;
            set
            {
                if (value)
                    _configuration |= (1 << 18);
                else
                    _configuration &= ~(1 << 18);
            }
        }

        /// <summary>
        /// Convenience accessor for the NPC's current logical state without requiring a
        /// <see cref="NonPlayerCharacterDataDefinition"/> reference.
        /// Reads bits 0–3 of <c>_condition</c> (the NPC_STATE field).
        /// </summary>
        public ENPCState State => (ENPCState)(_condition & 0x0F);

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }

        public byte SetBitNoRef(byte flags, int bit, bool value)
        {
            if (value)
            {
                return flags |= (byte)(1 << bit);
            }
            else
            {
                return flags &= unchecked((byte)~(1 << bit));
            }
        }
    }
}
