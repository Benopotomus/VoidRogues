using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.Core
{
    /// <summary>
    /// Generic object pool. Reuses inactive GameObjects instead of
    /// instantiating and destroying them every frame.
    /// Required for all projectiles and frequently spawned VFX.
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _available = new Queue<T>();

        /// <param name="prefab">The component on the pooled prefab.</param>
        /// <param name="parent">Optional parent transform for pooled objects.</param>
        /// <param name="initialSize">Number of instances to pre-warm.</param>
        public ObjectPool(T prefab, Transform parent = null, int initialSize = 10)
        {
            _prefab = prefab;
            _parent = parent;
            for (int i = 0; i < initialSize; i++)
                _available.Enqueue(CreateNew());
        }

        /// <summary>Get an instance from the pool (activates it).</summary>
        public T Get(Vector3 position, Quaternion rotation)
        {
            T obj = _available.Count > 0 ? _available.Dequeue() : CreateNew();
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>Return an instance to the pool (deactivates it).</summary>
        public void Return(T obj)
        {
            obj.gameObject.SetActive(false);
            _available.Enqueue(obj);
        }

        private T CreateNew()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
