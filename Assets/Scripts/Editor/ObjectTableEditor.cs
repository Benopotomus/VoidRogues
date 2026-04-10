using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DWD.Editor;

namespace VoidRogues.Editor
{
    public class ObjectTableEditor<T, L> : UnityEditor.Editor
        where T : TableObject
        where L : ObjectTable<T>
    {
        protected const string _PROP_TABLE = "_table";
        protected const string _PROP_ID = "_tableID";

        public string TableName { get { return typeof(L).Name; } }

        protected virtual string SearchParam { get { return "t:" + typeof(T).Name; } }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Rebuild " + TableName + " Table"))
            {
                CreateTableArray();
            }

            serializedObject.Update();

            SerializedProperty table = serializedObject.FindProperty(_PROP_TABLE);
            EditorGUILayout.PropertyField(table, true);

            serializedObject.ApplyModifiedProperties();
        }

        public virtual void CreateTableArray()
        {
            // Scrape the objects from the editor helpers
            List<T> tableObject = EditorHelpers.Scrape<T>(SearchParam);

            // Get the serialized property for the table
            SerializedProperty table = serializedObject.FindProperty(_PROP_TABLE);

            // Increase the array size by 1 to make room for the new object at index 0
            int count = tableObject.Count;
            table.arraySize = count + 1;

            // Inject a new object at index 0 (you can customize this as needed)
            SerializedProperty firstElement = table.GetArrayElementAtIndex(0);
            T injectedObject = default(T); // Replace with your logic to create or fetch the injected object
            firstElement.objectReferenceValue = injectedObject;

            // Populate the rest of the array, shifting everything up by one
            for (int a = 0; a < count; a++)
            {
                SerializedProperty element = table.GetArrayElementAtIndex(a + 1); // Shift index by 1
                T obj = tableObject[a];
                element.objectReferenceValue = obj;

                // Update the ID of the object if it has one
                SerializedObject tableSO = new SerializedObject(obj);
                if (tableSO != null)
                {
                    tableSO.Update();
                    SerializedProperty id = tableSO.FindProperty(_PROP_ID);
                    if (id != null) id.intValue = a + 1; // IDs now match their new indices (shifted up by 1)
                    tableSO.ApplyModifiedProperties();
                }
            }

            // Apply all changes to the serialized object
            serializedObject.ApplyModifiedProperties();
        }
    }
}