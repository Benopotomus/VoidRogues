using Fusion;
using System.Runtime.InteropServices;

namespace VoidRogues
{
    /// <summary>
    /// Networked item data stub. Mirrors the LichLord FItemData layout (4 bytes).
    /// Expand as the item system is built out.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct FItemData : INetworkStruct
    {
        [FieldOffset(0)]
        public int RawData;

        public int DefinitionID
        {
            get => RawData & 0xFFFF;
            set => RawData = (RawData & ~0xFFFF) | (value & 0xFFFF);
        }
    }
}
