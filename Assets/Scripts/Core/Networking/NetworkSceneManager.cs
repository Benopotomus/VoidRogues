namespace LichLord
{
    using System.Collections;
    using UnityEngine;
    using Fusion;

    using UnityScene = UnityEngine.SceneManagement.Scene;

    public class NetworkSceneManager : NetworkSceneManagerDefault
    {
        public NetworkedScene GameplayScene => _gameplayScene;

        private NetworkedScene _gameplayScene;

        private bool _isBusy;

        public override bool IsBusy => _isBusy | base.IsBusy;

        protected override IEnumerator OnSceneLoaded(SceneRef sceneRef, UnityScene scene, NetworkLoadSceneParameters sceneParams)
        {
            Debug.Log("NetworkSceneManager SceneLoaded");
            _isBusy = true;
            _gameplayScene = scene.GetComponent<NetworkedScene>(true);

            float contextTimeout = 20.0f;
            while (_gameplayScene.ContextReady == false && contextTimeout > 0.0f)
            {
                yield return null;
                contextTimeout -= Time.unscaledDeltaTime;
            }

            // Assign Context
            var contextBehaviours = scene.GetComponents<IContextBehaviour>(true);
            foreach (var behaviour in contextBehaviours)
            {
                behaviour.Context = _gameplayScene.Context;
            }

            yield return base.OnSceneLoaded(sceneRef, scene, sceneParams);

            _isBusy = false;
        }
    }
}
