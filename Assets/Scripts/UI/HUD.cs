using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidRogues.GameFlow;

namespace VoidRogues.UI
{
    /// <summary>
    /// Main HUD displayed during a mission.
    /// Reads from the local player's <see cref="VoidRogues.Player.PlayerController"/>
    /// and updates health / ammo displays each frame.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private Slider    _healthBar;
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Wave Info")]
        [SerializeField] private TextMeshProUGUI _waveText;
        [SerializeField] private TextMeshProUGUI _enemyCountText;

        private Player.PlayerController _localPlayer;
        private EnemyManager            _enemyManager;

        private void Start()
        {
            GameManager.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            GameManager.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameManager.GameState state)
        {
            gameObject.SetActive(state == GameManager.GameState.InMission);
        }

        private void Update()
        {
            if (_localPlayer == null)
            {
                TryFindLocalPlayer();
                return;
            }

            if (_enemyManager == null)
            {
                _enemyManager = FindObjectOfType<EnemyManager>();
            }

            UpdateHealthDisplay();
            UpdateEnemyDisplay();
        }

        private void TryFindLocalPlayer()
        {
            // Find the PlayerController that has input authority over the local machine.
            var players = FindObjectsOfType<Player.PlayerController>();
            foreach (var p in players)
            {
                if (p.Object != null && p.Object.HasInputAuthority)
                {
                    _localPlayer = p;
                    return;
                }
            }
        }

        private void UpdateHealthDisplay()
        {
            float normalised = _localPlayer.Health / 100f;
            if (_healthBar  != null) _healthBar.value = normalised;
            if (_healthText != null) _healthText.text  = _localPlayer.Health.ToString();
        }

        private void UpdateEnemyDisplay()
        {
            if (_enemyManager == null) return;
            if (_enemyCountText != null)
            {
                _enemyCountText.text = $"Enemies: {_enemyManager.ActiveEnemyCount}";
            }
        }
    }
}
