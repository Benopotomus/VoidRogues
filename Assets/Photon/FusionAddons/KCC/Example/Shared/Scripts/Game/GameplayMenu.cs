namespace Example
{
	using System;
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using UnityEngine.SceneManagement;
	using Fusion;
	using Fusion.Addons.KCC;
	using Example.ExpertMovement;

	/// <summary>
	/// Helper script for UI and keyboard shortcuts.
	/// </summary>
	public sealed class GameplayMenu : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private bool             _showGUI;
		[SerializeField]
		private GUISkin          _skin;
		[SerializeField]
		private SceneConfig      _sceneConfig;
		[SerializeField]
		private FrameRateUpdater _frameRateUpdater;

		private ExpertPlayer _localPlayer;
		private GUIStyle     _defaultStyle;
		private GUIStyle     _activeStyle;
		private GUIStyle     _inactiveStyle;
		private bool         _enableRecording;
		private double[]     _roundTripTimes = new double[100];
		private int          _averageRTT;

		// MonoBehaviour INTERFACE

		private void Update()
		{
			if (_showGUI == true && Runner != null)
			{
				_roundTripTimes[Time.frameCount % _roundTripTimes.Length] = Runner.GetPlayerRtt(PlayerRef.None);

				double averageRTT = 0.0;
				for (int i = 0, count = _roundTripTimes.Length; i < count; ++i)
				{
					averageRTT += _roundTripTimes[i];
				}

				_averageRTT = Mathf.RoundToInt((float)(averageRTT * (1000.0 / _roundTripTimes.Length)));
			}

			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
				return;

			if (keyboard.f4Key.wasPressedThisFrame == true)
			{
				ToggleInputSmoothing();
			}

			if (keyboard.f5Key.wasPressedThisFrame == true)
			{
				ToggleFrameRate();
			}

			if (keyboard.f6Key.wasPressedThisFrame == true)
			{
				ToggleQualityLevel();
			}

			if (keyboard.f7Key.wasPressedThisFrame == true)
			{
				ToggleVSync();
			}

			if (keyboard.f8Key.wasPressedThisFrame == true && Application.isMobilePlatform == false && Application.isEditor == false)
			{
				ToggleFullScreen();
			}

			if (keyboard.f9Key.wasPressedThisFrame == true)
			{
				ToggleRecording();
			}

			if (keyboard.f12Key.wasPressedThisFrame == true)
			{
				Disconnect();
			}

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (keyboard.enterKey.wasPressedThisFrame == true || keyboard.numpadEnterKey.wasPressedThisFrame == true)
				{
					ToggleCursor();
				}
			}
		}

		private void OnGUI()
		{
			if (_showGUI == false)
				return;

			Initialize();

			if (Runner == null || Runner.IsRunning == false)
				return;

			bool hasLocalPlayer = HasLocalPlayer();

			float verticalSpace   = 5.0f;
			float horizontalSpace = 5.0f;

			GUILayout.BeginVertical();
			GUILayout.Space(verticalSpace);
			GUILayout.BeginHorizontal();
			GUILayout.Space(horizontalSpace);

			GUILayout.Button($"{Mathf.RoundToInt(1.0f / Runner.DeltaTime)}Hz", _defaultStyle);

			if (_averageRTT > 0)
			{
				Vector2 rttSize = GUI.skin.label.CalcSize(new GUIContent("000ms"));
				GUILayout.Button($"{_averageRTT}ms", _defaultStyle, GUILayout.Width(40.0f + rttSize.x));
				GUILayout.Button($"{Runner.CurrentConnectionType}", _defaultStyle);
			}

			if (hasLocalPlayer == true)
			{
				float    playerSpeed         = GetPlayerSpeed();
				GUIStyle playerSpeedStyle    = playerSpeed.AlmostEquals(1.0f) == true ? _defaultStyle : (playerSpeed > 1.0f ? _activeStyle : _inactiveStyle);
				float    inputSmoothing      = GetInputSmoothing();
				GUIStyle inputSmoothingStyle = inputSmoothing.IsAlmostZero(0.000001f) == true ? _defaultStyle : (inputSmoothing < (1.0f / 30.0f) ? _activeStyle : _inactiveStyle);

				if (GUILayout.Button($"[+/-] Speed ({playerSpeed:F2}x)", playerSpeedStyle) == true)
				{
					TogglePlayerSpeed();
				}

				if (GUILayout.Button($"[F4] Smoothing ({(int)(inputSmoothing * 1000.0f + 0.1f)}ms)", inputSmoothingStyle) == true)
				{
					ToggleInputSmoothing();
				}
			}

			string   frameRate      = Application.targetFrameRate == 0 ? "Unlimited" : Application.targetFrameRate.ToString();
			GUIStyle frameRateStyle = Application.targetFrameRate == 0 ? _defaultStyle : _activeStyle;

			if (GUILayout.Button($"[F5] FPS ({frameRate} / {_frameRateUpdater.SmoothFrameRate})", frameRateStyle) == true)
			{
				ToggleFrameRate();
			}

			string qualityName  = "Default";
			int    qualityLevel = _sceneConfig.GetQualityLevel();

			switch (qualityLevel)
			{
				case 0: { qualityName = "Low";    break; }
				case 1: { qualityName = "Medium"; break; }
				case 2: { qualityName = "High";   break; }
			}

			if (GUILayout.Button($"[F6] Quality: {qualityName}", _defaultStyle) == true)
			{
				ToggleQualityLevel();
			}

			if (GUILayout.Button($"[F7] V-Sync ({(QualitySettings.vSyncCount == 0 ? "Off" : "On")})", QualitySettings.vSyncCount == 0 ? _defaultStyle : _activeStyle) == true)
			{
				ToggleVSync();
			}

			if (Application.isMobilePlatform == false && Application.isEditor == false)
			{
				if (GUILayout.Button($"[F8] FullScreen ({Screen.fullScreenMode})", _defaultStyle) == true)
				{
					ToggleFullScreen();
				}
			}

			if (GUILayout.Button($"[F9] Recording ({(_enableRecording == true ? "On" : "Off")})", _enableRecording == true ? _activeStyle : _defaultStyle) == true)
			{
				ToggleRecording();
			}

			if (GUILayout.Button($"[F12] Disconnect", _defaultStyle) == true)
			{
				Disconnect();
			}

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (GUILayout.Button($"[Enter] Cursor Lock", Cursor.lockState == CursorLockMode.Locked ? _activeStyle : _defaultStyle) == true)
				{
					ToggleCursor();
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		// PRIVATE METHODS

		private void Initialize()
		{
			if (_defaultStyle == null)
			{
				_defaultStyle = new GUIStyle(_skin.button);
				_defaultStyle.alignment = TextAnchor.MiddleCenter;

				if (Application.isMobilePlatform == true && Application.isEditor == false)
				{
					_defaultStyle.fontSize = 20;
					_defaultStyle.padding = new RectOffset(20, 20, 20, 20);
				}

				_activeStyle = new GUIStyle(_defaultStyle);
				_activeStyle.normal.textColor  = Color.green;
				_activeStyle.focused.textColor = Color.green;
				_activeStyle.hover.textColor   = Color.green;

				_inactiveStyle = new GUIStyle(_defaultStyle);
				_inactiveStyle.normal.textColor  = Color.red;
				_inactiveStyle.focused.textColor = Color.red;
				_inactiveStyle.hover.textColor   = Color.red;
			}
		}

		private bool HasLocalPlayer()
		{
			return GetLocalPlayer() != null;
		}

		private float GetPlayerSpeed()
		{
			ExpertPlayer player = GetLocalPlayer();
			if (player == null)
				return 1.0f;

			return player.SpeedMultiplier;
		}

		private void TogglePlayerSpeed()
		{
			ExpertPlayer player = GetLocalPlayer();
			if (player == null)
				return;

			player.ToggleSpeedRPC(1);
		}

		private void ToggleFrameRate()
		{
			_frameRateUpdater.Toggle();
		}

		private void ToggleVSync()
		{
			QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
		}

		private float GetInputSmoothing()
		{
			ExpertPlayer player = GetLocalPlayer();
			if (player == null)
				return 0.0f;

			return player.Input.LookResponsivity;
		}

		private void ToggleInputSmoothing()
		{
			ExpertPlayer player = GetLocalPlayer();
			if (player == null)
				return;

			switch (player.Input.LookResponsivity)
			{
				case 0.000f : { player.Input.LookResponsivity = 0.005f; break; }
				case 0.005f : { player.Input.LookResponsivity = 0.010f; break; }
				case 0.010f : { player.Input.LookResponsivity = 0.015f; break; }
				case 0.015f : { player.Input.LookResponsivity = 0.020f; break; }
				case 0.020f : { player.Input.LookResponsivity = 0.025f; break; }
				case 0.025f : { player.Input.LookResponsivity = 0.035f; break; }
				case 0.035f : { player.Input.LookResponsivity = 0.050f; break; }
				case 0.050f : { player.Input.LookResponsivity = 0.075f; break; }
				case 0.075f : { player.Input.LookResponsivity = 0.100f; break; }
				case 0.100f : { player.Input.LookResponsivity = 0.000f; break; }
				default     : { player.Input.LookResponsivity = 0.000f; break; }
			}
		}

		private void ToggleFullScreen()
		{
			Resolution maxResolution            = default;
			int        maxResolutionSize        = default;
			int        maxResolutionRefreshRate = default;

			Resolution[] resolutions = Screen.resolutions;
			foreach (Resolution resolution in resolutions)
			{
				int resolutionSize = resolution.width * resolution.height;
				if (resolutionSize >= maxResolutionSize)
				{
					if (ApplicationUtility.GetRefreshRate(resolution) >= maxResolutionRefreshRate)
					{
						maxResolutionSize        = resolutionSize;
						maxResolutionRefreshRate = ApplicationUtility.GetRefreshRate(resolution);
						maxResolution            = resolution;
					}
				}
			}

			switch (Screen.fullScreenMode)
			{
				case FullScreenMode.ExclusiveFullScreen: { ApplicationUtility.SetResolution(maxResolution.width / 2, maxResolution.height / 2, FullScreenMode.Windowed,            ApplicationUtility.GetRefreshRate(maxResolution)); break;}
				case FullScreenMode.FullScreenWindow:    { ApplicationUtility.SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.ExclusiveFullScreen, ApplicationUtility.GetRefreshRate(maxResolution)); break;}
				case FullScreenMode.MaximizedWindow:     { ApplicationUtility.SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.FullScreenWindow,    ApplicationUtility.GetRefreshRate(maxResolution)); break;}
				case FullScreenMode.Windowed:            { ApplicationUtility.SetResolution(maxResolution.width,     maxResolution.height,     FullScreenMode.MaximizedWindow,     ApplicationUtility.GetRefreshRate(maxResolution)); break;}
				default:
				{
					throw new NotImplementedException(Screen.fullScreenMode.ToString());
				}
			}
		}

		private void ToggleQualityLevel()
		{
			if (_sceneConfig == null)
				return;

			_sceneConfig.SetQualityLevel(_sceneConfig.GetQualityLevel() + 1);
		}

		private void ToggleCursor()
		{
			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Cursor.lockState = CursorLockMode.None;
					Cursor.visible = true;
				}
				else
				{
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible = false;
				}
			}
		}

		private void ToggleRecording()
		{
			if (Runner == null)
				return;

			List<StatsRecorder> recorders = Runner.GetAllBehaviours<StatsRecorder>();
			if (recorders == null || recorders.Count == 0)
			{
				_enableRecording = false;
				return;
			}

			_enableRecording = !_enableRecording;

			foreach (StatsRecorder recorder in recorders)
			{
				recorder.SetActive(_enableRecording);
			}
		}

		private void Disconnect()
		{
			Cursor.lockState = CursorLockMode.None;
			Cursor.visible   = true;

#if UNITY_2023_1_OR_NEWER
			FusionBootstrap bootstrap = GameObject.FindAnyObjectByType<FusionBootstrap>();
#else
			FusionBootstrap bootstrap = GameObject.FindObjectOfType<FusionBootstrap>();
#endif
			if (bootstrap != null)
			{
				bootstrap.ShutdownAll();
				return;
			}

			if (Runner != null)
			{
				Runner.Shutdown();
				SceneManager.LoadScene("Startup");
			}
		}

		private bool GetNumberDown(int offset)
		{
			switch (offset)
			{
				case 0 : { return Keyboard.current.numpad1Key.wasPressedThisFrame == true || Keyboard.current.digit1Key.wasPressedThisFrame == true; }
				case 1 : { return Keyboard.current.numpad2Key.wasPressedThisFrame == true || Keyboard.current.digit2Key.wasPressedThisFrame == true; }
				case 2 : { return Keyboard.current.numpad3Key.wasPressedThisFrame == true || Keyboard.current.digit3Key.wasPressedThisFrame == true; }
				case 3 : { return Keyboard.current.numpad4Key.wasPressedThisFrame == true || Keyboard.current.digit4Key.wasPressedThisFrame == true; }
				case 4 : { return Keyboard.current.numpad5Key.wasPressedThisFrame == true || Keyboard.current.digit5Key.wasPressedThisFrame == true; }
				case 5 : { return Keyboard.current.numpad6Key.wasPressedThisFrame == true || Keyboard.current.digit6Key.wasPressedThisFrame == true; }
				case 6 : { return Keyboard.current.numpad7Key.wasPressedThisFrame == true || Keyboard.current.digit7Key.wasPressedThisFrame == true; }
				case 7 : { return Keyboard.current.numpad8Key.wasPressedThisFrame == true || Keyboard.current.digit8Key.wasPressedThisFrame == true; }
				case 8 : { return Keyboard.current.numpad9Key.wasPressedThisFrame == true || Keyboard.current.digit9Key.wasPressedThisFrame == true; }
			}

			return false;
		}

		private ExpertPlayer GetLocalPlayer()
		{
			if (Runner == null)
				return default;

			PlayerRef localPlayerRef = Runner.LocalPlayer;
			if (localPlayerRef.IsNone == true)
				return default;

			if (_localPlayer == null)
			{
				_localPlayer = null;

				NetworkObject localPlayerObject = Runner.GetPlayerObject(localPlayerRef);
				if (localPlayerObject != null)
				{
					_localPlayer = localPlayerObject.GetComponent<ExpertPlayer>();
				}
			}

			return _localPlayer;
		}
	}
}
