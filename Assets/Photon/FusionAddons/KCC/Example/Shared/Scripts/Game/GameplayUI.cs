namespace Example
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using UnityEngine.UI;
	using Fusion;
	using Fusion.Addons.KCC;
	using Example.ExpertMovement;

	/// <summary>
	/// Shows information related to gameplay.
	/// </summary>
	public sealed class GameplayUI : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _mapStatus;
		[SerializeField]
		private Text       _mapStatusText;
		[SerializeField]
		private GameObject _crosshair;
		[SerializeField]
		private GameObject _funIndicator;
		[SerializeField]
		private GameObject _renderIndicator;
		[SerializeField]
		private GameObject _standaloneControls;
		[SerializeField]
		private GameObject _mobileControls;
		[SerializeField]
		private GameObject _gamepadControls;

		private PlayerRef _localPlayer;
		private KCC       _localPlayerKCC;
		private bool      _enableFUNIndicator;
		private bool      _enableRenderIndicator;
		private float     _indicatorsResetTime;

		private List<IMapStatusProvider> _statusProviders = new List<IMapStatusProvider>();

		// PUBLIC MEMBERS

		public void SetCrosshairActive(bool isActive)
		{
			_crosshair.SetActive(isActive);
		}

		public void SetReflexIndicatorActive()
		{
			if (Runner.Stage == SimulationStages.Resimulate)
				return;

			if (Runner.Stage == SimulationStages.Forward)
			{
				_enableFUNIndicator = true;
			}

			_enableRenderIndicator = true;
			_indicatorsResetTime = 0.1f;
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_mapStatus.SetActive(false);

			RefreshControls(false);
		}

		private void Update()
		{
			string mapStatus = default;

			if (GetLocalPlayerKCC(out KCC localPlayerKCC, out PlayerRef localPlayer, out bool hasUpdated) == true)
			{
				RefreshControls(true);

				// Get all processors from KCC that implement IMapStatusProvider.
				localPlayerKCC.GetProcessors<IMapStatusProvider>(_statusProviders);

				// Iterate over processors and try to collect status.
				for (int i = 0; i < _statusProviders.Count; ++i)
				{
					IMapStatusProvider statusProvider = _statusProviders[i];
					if (statusProvider.IsActive(localPlayer) == true)
					{
						mapStatus = statusProvider.GetStatus(localPlayer);
						break;
					}
				}

				if (mapStatus == default)
				{
					// Following is an example of incorrect usage (unless you 100% know what you are doing):
					// No status provided from processors above, let's try to iterate over all collision hits.
					// This approach works well until is a condition which defer a processor from starting the interaction (controlled by overriding IKCCInteractionProvider.CanStartInteraction()),
					// you'll see map status even if the interaction not yet started.
					// Please be careful when processing raw collision hits.

					KCCHits hits = localPlayerKCC.Data.Hits;
					for (int i = 0; i < hits.Count; ++i)
					{
						IMapStatusProvider statusProvider = hits.All[i].Transform.GetComponentNoAlloc<IMapStatusProvider>();
						if (statusProvider != null && statusProvider.IsActive(localPlayer) == true)
						{
							mapStatus = statusProvider.GetStatus(localPlayer);
							break;
						}
					}
				}
			}

			if (string.IsNullOrEmpty(mapStatus) == true)
			{
				_mapStatus.SetActive(false);
			}
			else
			{
				_mapStatus.SetActive(true);
				_mapStatusText.text = mapStatus;
			}

			if (hasUpdated == true)
			{
				SetCrosshairActive(localPlayerKCC != null && localPlayerKCC.GetComponent<FirstPersonExpertPlayer>() != null);
			}
		}

		private void LateUpdate()
		{
			_funIndicator.SetActive(_enableFUNIndicator);
			_renderIndicator.SetActive(_enableRenderIndicator);

			if (_indicatorsResetTime > 0.0f)
			{
				_indicatorsResetTime -= Time.unscaledDeltaTime;

				if (_indicatorsResetTime <= 0.0f)
				{
					_enableFUNIndicator    = default;
					_enableRenderIndicator = default;
					_indicatorsResetTime   = default;
				}
			}
		}

		// PRIVATE METHODS

		private void RefreshControls(bool hasPlayer)
		{
			if (hasPlayer == false)
			{
				if (_standaloneControls != null) { _standaloneControls.SetActive(false); }
				if (_mobileControls     != null) { _mobileControls.SetActive(false);     }
				if (_gamepadControls    != null) { _gamepadControls.SetActive(false);    }

				return;
			}

			if (Application.isMobilePlatform == true && Application.isEditor == false)
			{
				if (_standaloneControls != null) { _standaloneControls.SetActive(false); }
				if (_mobileControls     != null) { _mobileControls.SetActive(true);      }
			}
			else
			{
				if (_standaloneControls != null) { _standaloneControls.SetActive(true); }
				if (_mobileControls     != null) { _mobileControls.SetActive(false);    }
			}

			if (Gamepad.current != null)
			{
				if (_gamepadControls != null) { _gamepadControls.SetActive(true); }
			}
		}

		private bool GetLocalPlayerKCC(out KCC localPlayerKCC, out PlayerRef localPlayer, out bool hasUpdated)
		{
			hasUpdated     = false;
			localPlayer    = _localPlayer;
			localPlayerKCC = _localPlayerKCC;

			if (localPlayerKCC != null)
				return true;

			if (Runner == null)
				return false;

			localPlayer = Runner.LocalPlayer;
			_localPlayer = localPlayer;

			if (localPlayer.IsNone == true)
				return false;

			if (Runner.TryGetPlayerObject(localPlayer, out NetworkObject localPlayerObject) == false || localPlayerObject == null)
				return false;

			localPlayerKCC = localPlayerObject.GetComponentNoAlloc<KCC>();
			_localPlayerKCC = localPlayerKCC;

			hasUpdated = true;

			return localPlayerKCC != null;
		}
	}
}
