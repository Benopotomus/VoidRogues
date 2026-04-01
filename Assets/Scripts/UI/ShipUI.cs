using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VoidRogues.GameFlow;

namespace VoidRogues.UI
{
    /// <summary>
    /// UI controller for the Ship lobby / mission-select screen.
    ///
    /// Attach to the Bridge room's Canvas in the Ship scene.
    /// </summary>
    public class ShipUI : MonoBehaviour
    {
        [Header("Ready Button")]
        [SerializeField] private Button _readyButton;
        [SerializeField] private TextMeshProUGUI _readyButtonLabel;

        [Header("Player Slots")]
        [SerializeField] private PlayerSlotUI[] _playerSlots;

        private ShipManager _shipManager;
        private NetworkRunner _runner;
        private bool _isReady;

        private void Start()
        {
            _runner      = FindObjectOfType<NetworkRunner>();
            _shipManager = FindObjectOfType<ShipManager>();

            if (_readyButton != null)
                _readyButton.onClick.AddListener(OnReadyClicked);
        }

        private void Update()
        {
            if (_shipManager == null) return;

            for (int i = 0; i < _playerSlots.Length; i++)
            {
                if (_playerSlots[i] == null) continue;
                _playerSlots[i].SetReady(_shipManager.IsPlayerReady(i));
            }
        }

        private void OnReadyClicked()
        {
            _isReady = !_isReady;

            if (_readyButtonLabel != null)
                _readyButtonLabel.text = _isReady ? "Cancel" : "Ready";

            _shipManager?.RPC_SetReady(_isReady);
        }
    }

    /// <summary>Small UI widget showing a single player's ready state.</summary>
    [System.Serializable]
    public class PlayerSlotUI
    {
        public GameObject ReadyIndicator;

        public void SetReady(bool ready)
        {
            if (ReadyIndicator != null)
                ReadyIndicator.SetActive(ready);
        }
    }
}
