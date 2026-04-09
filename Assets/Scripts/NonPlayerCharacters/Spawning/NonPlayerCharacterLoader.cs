// using DWD.Utility.Loading; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [System.Serializable]
    public class NonPlayerCharacterLoader
    {
        private FNonPlayerCharacterSpawnParams _spawnParams;
        public FNonPlayerCharacterSpawnParams SpawnParams => _spawnParams;

        // TODO: Port AssetBundleLoader from LichLord
        // private AssetBundleLoader _loader;

        private GameObject _loadedPrefab;
        public GameObject LoadedPrefab { get { return _loadedPrefab; } }
        public System.Action<NonPlayerCharacterLoader> OnLoadComplete;

        public NonPlayerCharacterLoader() { }
    }
}
