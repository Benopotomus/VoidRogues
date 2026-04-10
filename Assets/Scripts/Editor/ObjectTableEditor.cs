using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
            List<T> tableObject = Scrape<T>(SearchParam);

            SerializedProperty table = serializedObject.FindProperty(_PROP_TABLE);

            int count = tableObject.Count;
            table.arraySize = count + 1;

            // Index 0 is reserved (null sentinel)
            SerializedProperty firstElement = table.GetArrayElementAtIndex(0);
            firstElement.objectReferenceValue = null;

            for (int a = 0; a < count; a++)
            {
                SerializedProperty element = table.GetArrayElementAtIndex(a + 1);
                T obj = tableObject[a];
                element.objectReferenceValue = obj;

                SerializedObject tableSO = new SerializedObject(obj);
                if (tableSO != null)
                {
                    tableSO.Update();
                    SerializedProperty id = tableSO.FindProperty(_PROP_ID);
                    if (id != null) id.intValue = a + 1;
                    tableSO.ApplyModifiedProperties();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static List<TAsset> Scrape<TAsset>(string filter) where TAsset : Object
        {
            List<TAsset> results = new List<TAsset>();
            string[] guids = AssetDatabase.FindAssets(filter);
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                TAsset asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
                if (asset != null)
                    results.Add(asset);
            }
            return results;
        }
    }
}