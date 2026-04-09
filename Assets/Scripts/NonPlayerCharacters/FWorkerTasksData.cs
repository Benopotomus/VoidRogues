using Fusion;
using System.Runtime.InteropServices;

namespace VoidRogues
{
    [StructLayout(LayoutKind.Explicit, Size = 1)]
    public struct FWorkerTasksData : INetworkStruct
    {
        [FieldOffset(0)]
        private byte _data;

        public bool Task0 { get { return IsBitSet(ref _data, 1); } set { SetBit(ref _data, 1, value); } }
        public bool Task1 { get { return IsBitSet(ref _data, 2); } set { SetBit(ref _data, 2, value); } }
        public bool Task2 { get { return IsBitSet(ref _data, 3); } set { SetBit(ref _data, 3, value); } }
        public bool Task3 { get { return IsBitSet(ref _data, 4); } set { SetBit(ref _data, 4, value); } }
        public bool Task4 { get { return IsBitSet(ref _data, 5); } set { SetBit(ref _data, 5, value); } }
        public bool Task5 { get { return IsBitSet(ref _data, 6); } set { SetBit(ref _data, 6, value); } }
        public bool Task6 { get { return IsBitSet(ref _data, 7); } set { SetBit(ref _data, 7, value); } }
        public bool Task7 { get { return IsBitSet(ref _data, 8); } set { SetBit(ref _data, 8, value); } }

        public byte RawData
        {
            get => _data;
            set => _data = value;
        }

        public bool IsTaskActive(int index)
        {
            switch (index)
            {
                case 0: return Task0;
                case 1: return Task1;
                case 2: return Task2;
                case 3: return Task3;
                case 4: return Task4;
                case 5: return Task5;
                case 6: return Task6;
                case 7: return Task7;
                default: return false;
            }
        }

        public void ToggleTask(int index)
        {
            switch (index)
            {
                case 0: Task0 = !Task0; break;
                case 1: Task1 = !Task1; break;
                case 2: Task2 = !Task2; break;
                case 3: Task3 = !Task3; break;
                case 4: Task4 = !Task4; break;
                case 5: Task5 = !Task5; break;
                case 6: Task6 = !Task6; break;
                case 7: Task7 = !Task7; break;
            }
        }

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
