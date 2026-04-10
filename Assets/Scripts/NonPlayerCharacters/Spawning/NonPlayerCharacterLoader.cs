using DWD.Utility.Loading;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    [System.Serializable]
    public class NonPlayerCharacterLoader
    {
        private FNonPlayerCharacterSpawnParams _spawnParams;
        public FNonPlayerCharacterSpawnParams SpawnParams => _spawnParams;

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
        public NonPlayerCharacterLoader(FNonPlayerCharacterSpawnParams spawnParams,
            AssetBundleLoader iLoader)
        {
            _spawnParams.Copy(spawnParams);
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
