using Fusion;
// using LichLord.World; // TODO: Port from LichLord
using System.Runtime.InteropServices;

namespace VoidRogues
{
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public struct FWorkerData : INetworkStruct
    {
        [FieldOffset(0)]
        private byte _state;
        [FieldOffset(1)]
        public FWorkerTasksData TasksData;
        [FieldOffset(2)]
        public short NPCIndex;
        // [FieldOffset(4)]
        // public FStaticPropPosition TargetNode; // TODO: Port FStaticPropPosition from LichLord

        public bool IsAssigned { get { return IsBitSet(ref _state, 1); } set { SetBit(ref _state, 1, value); } }
        public bool WorkerActive { get { return IsBitSet(ref _state, 2); } set { SetBit(ref _state, 2, value); } }
        public bool HasTargetNode { get { return IsBitSet(ref _state, 3); } set { SetBit(ref _state, 3, value); } }

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
