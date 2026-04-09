
// This is placed in the level and loaded that way.

namespace VoidRogues
{
    using System.Collections;
    using Fusion;
    using UnityScene = UnityEngine.SceneManagement.Scene;

    public class GameplayScene : NetworkedScene
    {
        private const string UI_SCENE_NAME = "GameplayUI";

        // PRIVATE MEMBERS

        private UnityScene _UIScene;

        // Scene INTERFACE

        protected override IEnumerator OnActivate()
        {
            yield return base.OnActivate();

            // Adding the UI service for gameplay scene on activate.
            //AddService(Context.UI);
            //Context.UI.Activate();

            Context.Camera.Activate();
        }

    }
}
