namespace Example
{
	using UnityEngine;
	using Fusion;
	using Fusion.Menu;
	using TMPro;

	public class MenuUIController : FusionMenuUIController
	{
		[SerializeField]
		private TMP_Dropdown _gameMode;

		public FusionMenuConfig Config => _config;

		public GameMode SelectedGameMode { get; private set; } = GameMode.AutoHostOrClient;

		public void OnGameStarted()
		{
			foreach (Transform child in transform)
			{
				child.gameObject.SetActive(false);
			}
		}

		public void OnGameStopped()
		{
			foreach (Transform child in transform)
			{
				child.gameObject.SetActive(true);
			}
		}

		protected override void Awake()
		{
			base.Awake();

			Cursor.lockState = CursorLockMode.None;
			Cursor.visible = true;

			int gameMode = PlayerPrefs.GetInt("MenuUI.GameMode", 0);
			SetGameMode(gameMode);

			if (_gameMode != null)
			{
				_gameMode.value = gameMode;
				_gameMode.onValueChanged.AddListener(SetGameMode);
			}
		}

		private void SetGameMode(int value)
		{
			if (value == 1)
			{
				SelectedGameMode = GameMode.Shared;
			}
			else
			{
				value = 0;
				SelectedGameMode = GameMode.AutoHostOrClient;
			}

			PlayerPrefs.SetInt("MenuUI.GameMode", value);
		}
	}
}
