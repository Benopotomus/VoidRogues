using Fusion;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VoidRogues
{
    [StructLayout(LayoutKind.Explicit)]
    public struct FCommandedUnitStats : INetworkStruct
    {
        [FieldOffset(0)]
        private int _data;

        private const int DEFINITION_BITS = 7;
        private const int DEFINITION_MASK = (1 << DEFINITION_BITS) - 1;
        private const int DEFINITION_SHIFT = 0;

        private const int ITEMDATA_SHIFT = DEFINITION_BITS;
        private const int ITEMDATA_MASK = ~DEFINITION_MASK;

        public bool IsValid() => DefinitionID != 0;

        public void Clear() => _data = 0;

        public int Data
        {
            get => _data;
            set => _data = value;
        }

        public int DefinitionID
        {
            get => (_data >> DEFINITION_SHIFT) & DEFINITION_MASK;
            set
            {
                int defValue = Mathf.Clamp(value, 0, DEFINITION_MASK);
                _data = (_data & ~DEFINITION_MASK) | (defValue << DEFINITION_SHIFT);
            }
        }

        public void Copy(in FCommandedUnitStats copied)
        {
            _data = copied._data;
        }

        public bool IsEqual(in FCommandedUnitStats other) => _data == other._data;
    }
}
