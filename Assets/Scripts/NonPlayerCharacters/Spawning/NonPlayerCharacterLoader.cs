using DWD.Utility.Loading;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    [System.Serializable]
    public class NonPlayerCharacterLoader
    {
        private FNonPlayerCharacterData _data;
        public FNonPlayerCharacterData Data => _data;

        private int _index;
        public int Index => _index;

        private AssetBundleLoader _loader;
        public AssetBundleLoader Loader
        {
            get { return _loader; }
            set
            {
                _loader = value;
                _loader.OnLoadComplete += HandleLoaderComplete;
            }
        }

        private GameObject _loadedPrefab;
        public GameObject LoadedPrefab { get { return _loadedPrefab; } }
        public System.Action<NonPlayerCharacterLoader> OnLoadComplete;

        public NonPlayerCharacterLoader() { }
        public NonPlayerCharacterLoader( FNonPlayerCharacterData data, 
            int index,
            AssetBundleLoader iLoader)
        {
            _data.Copy(data);
            _index = index;
            Loader = iLoader;
        }

        private void HandleLoaderComplete(ILoader loader)
        {
            _loader.OnLoadComplete -= HandleLoaderComplete;
            _loadedPrefab = Loader.GetAsset<GameObject>();
            if (OnLoadComplete != null)
                OnLoadComplete.Invoke(this);
        }
    }
}
