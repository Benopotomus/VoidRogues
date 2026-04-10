

namespace VoidRogues.Editor
{
    using UnityEditor;
    using VoidRogues.NonPlayerCharacters;

    [CustomEditor(typeof(NonPlayerCharacterTable))]
    public class NonPlayerCharacterTableEditor : ObjectTableEditor<
        NonPlayerCharacterDefinition,
        NonPlayerCharacterTable>
    {

    }
}