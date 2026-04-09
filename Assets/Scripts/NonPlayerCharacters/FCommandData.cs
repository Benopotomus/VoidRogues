using Fusion;
using System.Runtime.InteropServices;

namespace VoidRogues
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FCommandData : INetworkStruct
    {
        [FieldOffset(0)]
        public byte ZoneID;
        [FieldOffset(1)]
        public ushort BuildableIndex;
        [FieldOffset(3)]
        private byte _state;

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool WorkerActive { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }

        public bool IsBitSet(ref byte flags, int bit)
        {
            return (flags & (1 << bit)) == (1 << bit);
        }

        public byte SetBit(ref byte flags, int bit, bool value)
        {
            if (value == true)
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
            if (value == true)
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
