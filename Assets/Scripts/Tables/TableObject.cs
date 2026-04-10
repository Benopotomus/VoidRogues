using UnityEngine;

namespace VoidRogues
{
    public class TableObject : ScriptableObject
    {
        [SerializeField, HideInInspector]
        private int _tableID = -1;
        public int TableID { get { return _tableID; } }
    }
}