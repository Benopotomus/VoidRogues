
namespace VoidRogues
{
    using UnityEngine;

    public abstract class ObjectTable <T>: ScriptableObject 
        where T : TableObject
    {
        [SerializeField]
        protected T[] _table = new T[0];
        public T[] Table { get { return _table; } }

        public T TryGetDefinition(int index)
        {
            if (index < 0 || index >= _table.Length)
                return null;
            return Table[index];
        }

        public virtual int TryGetID(T definition)
        {
            T def = definition;
            return def.TableID;
        }

        public virtual ulong GetRandomIndex()
        {
            return (ulong)UnityEngine.Random.Range(0, _table.Length);
        }

        public virtual T GetRandom()
        {
            return _table[GetRandomIndex()];
        }
    }
}