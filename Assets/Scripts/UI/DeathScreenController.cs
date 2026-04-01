using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VoidRogues.Core;

namespace VoidRogues.UI
{
    /// <summary>
    /// Shown when the player dies. Displays run statistics and provides
    /// buttons to restart or return to the main menu.
    /// </summary>
    public class DeathScreenController : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI roomsClearedText;
        [SerializeField] private TextMeshProUGUI enemiesKilledText;
        [SerializeField] private TextMeshProUGUI damageDealtText;
        [SerializeField] private TextMeshProUGUI itemsCollectedText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        private void Start()
        {
            PopulateStats();
            restartButton?.onClick.AddListener(OnRestartClicked);
            mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        }

        private void OnDestroy()
        {
            restartButton?.onClick.RemoveListener(OnRestartClicked);
            mainMenuButton?.onClick.RemoveListener(OnMainMenuClicked);
        }

        private void PopulateStats()
        {
            if (GameManager.Instance == null)
                return;

            var run = GameManager.Instance.Run;
            if (roomsClearedText  != null) roomsClearedText.text  = run.RoomsCleared.ToString();
            if (enemiesKilledText  != null) enemiesKilledText.text  = run.EnemiesKilled.ToString();
            if (damageDealtText    != null) damageDealtText.text    = run.TotalDamageDealt.ToString();
            if (itemsCollectedText != null) itemsCollectedText.text = run.CollectedItems.Count.ToString();
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.StartNewRun();
        }

        private void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.GoToMainMenu();
        }
    }
}
