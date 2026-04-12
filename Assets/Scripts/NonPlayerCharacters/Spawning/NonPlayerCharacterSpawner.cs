using DWD.Pooling;
using DWD.Utility.Loading;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterSpawner
    {
        public Action<FNonPlayerCharacterData, int, NonPlayerCharacter> OnPrefabSpawned;

        public void SpawnNPC(ref FNonPlayerCharacterData data, int index)
        {
            NonPlayerCharacterDefinition definition = Global.Tables.NonPlayerCharacterTable.TryGetDefinition(data.DefinitionID);

            if (definition == null)
            {
                Debug.LogWarning("Trying to spawn NPC with invalid definition, id: " + data.DefinitionID);
                return;
            }

            BundleObject prefabBundle = definition.PrefabBundle;

            if (!prefabBundle.Ready)
            {
                Debug.LogWarning("Cannot load null Bundle Object! ");
                return;
            }

            List<ILoader> LoadedBundles = AssetBundleManager.Instance.CompleteLoaders;

            for (int i = 0; i < LoadedBundles.Count; i++)
            {
                AssetBundleLoader loadedBundle = LoadedBundles[i] as AssetBundleLoader;

                if (loadedBundle.BundleName == prefabBundle.Bundle)
                {
                    OnPrefabLoaded(data, index, loadedBundle);
                    return;
                }
            }

            AssetBundleLoader prefabLoader = AssetBundleManager.Instance.LoadBundleObject(prefabBundle) as AssetBundleLoader;
            NonPlayerCharacterLoader npcLoader = new NonPlayerCharacterLoader(data, index, prefabLoader);

            if (npcLoader.Loader != null)
            {
                if (npcLoader.Loader.IsLoaded)
                    OnPrefabLoaded(npcLoader);
                else
                    npcLoader.OnLoadComplete += OnPrefabLoaded;
            }
        }

        private void OnPrefabLoaded(NonPlayerCharacterLoader loader)
        {
            loader.OnLoadComplete -= OnPrefabLoaded;
            OnPrefabLoaded(loader.Data, loader.Index, loader.Loader);
        }

        private void OnPrefabLoaded(FNonPlayerCharacterData data, int index, AssetBundleLoader loadedBundle)
        {
            GameObject prefab = loadedBundle.GetAssetWithin<GameObject>();

            if (prefab == null)
                return;

            var poolObject = prefab.GetComponent<DWDObjectPoolObject>();
            if (poolObject == null)
            {
                Debug.LogWarning("Could not spawn NPC " + data.DefinitionID + ". Could not find DWDObjectPoolObject Component!");
                return;
            }

            var instance = DWDObjectPool.Instance.SpawnAt(poolObject, data.Position, data.Rotation);

            NonPlayerCharacter spawnedNPC = instance.GetComponent<NonPlayerCharacter>();

            if (spawnedNPC == null)
            {
                Debug.LogWarning("NPC is Invalid, Check Bundles! (" + loadedBundle.BundleName + ")");
                return;
            }

            OnPrefabSpawned?.Invoke(data, index, spawnedNPC);
        }
    }
}