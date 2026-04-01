using UnityEngine;

namespace VoidRogues.Core
{
    /// <summary>
    /// Generic singleton base for manager classes.
    /// Persists across scene loads and destroys duplicates automatically.
    /// </summary>
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }
}
