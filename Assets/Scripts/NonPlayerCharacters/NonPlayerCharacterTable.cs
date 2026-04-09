using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// Static lookup table for NonPlayerCharacterDefinitions by TableID.
    /// Mirrors the LichLord Global.Tables.NonPlayerCharacterTable pattern.
    /// Populated at startup from a NonPlayerCharacterDefinition[] array (e.g. on the manager).
    /// </summary>
    public static class NonPlayerCharacterTable
    {
        private static NonPlayerCharacterDefinition[] _definitions;

        public static void Initialize(NonPlayerCharacterDefinition[] definitions)
        {
            _definitions = definitions;
        }

        public static NonPlayerCharacterDefinition TryGetDefinition(int tableID)
        {
            if (_definitions == null || tableID < 0 || tableID >= _definitions.Length)
                return null;

            return _definitions[tableID];
        }
    }
}
