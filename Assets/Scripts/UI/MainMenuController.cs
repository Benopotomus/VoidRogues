using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidRogues.Core;

namespace VoidRogues.UI
{
    /// <summary>
    /// Controls the Main Menu screen. Wires button callbacks and delegates
    /// game-start to <see cref="GameManager"/>.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TextMeshProUGUI versionText;

        private void Start()
        {
            if (versionText != null)
                versionText.text = $"v{Application.version}";

            startButton?.onClick.AddListener(OnStartClicked);
            quitButton?.onClick.AddListener(OnQuitClicked);
        }

        private void OnDestroy()
        {
            startButton?.onClick.RemoveListener(OnStartClicked);
            quitButton?.onClick.RemoveListener(OnQuitClicked);
        }

        private void OnStartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }

        private void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
