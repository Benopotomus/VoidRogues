namespace Example.InputSmoothing
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion.Addons.KCC;

	public sealed class InputSmoothingTest : MonoBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private int _inputBufferSize = 512;
		[SerializeField]
		private int[] _inputSmoothingsMs = new int[] { 0, 5, 10, 15, 20, 25, 30, 40, 50, 75, 100 };
		[SerializeField]
		private int[] _monitorRefreshRates = new int[] { 60, 120, 144, 165, 240, 288, 360 };
		[SerializeField]
		private Camera _camera;
		[SerializeField]
		private FrameRateUpdater _frameRateUpdater;
		[SerializeField]
		private GUISkin _skin;

		private List<InputTracker>   _inputTrackers = new List<InputTracker>();
		private List<MonitorTracker> _monitorTrackers = new List<MonitorTracker>();
		private StatsWriter          _timeWriter;
		private StatsWriter          _frameWriter;
		private GUIStyle             _defaultStyle;
		private GUIStyle             _activeStyle;
		private GUIStyle             _inactiveStyle;
		private Vector2              _cameraRotation;
		private int                  _baseFrameCount;
		private double               _baseUnscaledTime;
		private int                  _currentSmoothingIndex;
		private bool                 _enableRecording;
		private bool                 _isInitialized;

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_cameraRotation = _camera.transform.rotation.eulerAngles;

			ForEachSmoothing(AddInputTracker);
			ForEachMonitor(AddMonitorTracker);

			void AddInputTracker(int smoothingIndex)
			{
				_inputTrackers.Add(new InputTracker(_inputBufferSize, 0.001f * _inputSmoothingsMs[smoothingIndex]));
			}

			void AddMonitorTracker(int monitorIndex)
			{
				_monitorTrackers.Add(new MonitorTracker(_monitorRefreshRates[monitorIndex]));
			}

			ToggleCursor();
		}

		private void Update()
		{
			if (Time.frameCount < 10)
				return;

			ProcessKeyboardShortcuts();

			if (_enableRecording == true)
			{
				InitializeRecording();
			}
			else
			{
				DeinitializeRecording();
			}

			ForEachMonitor(ProcessMonitor);

			ForEachSmoothing(ProcessInput, Cursor.lockState == CursorLockMode.Locked ? Mouse.current.delta.ReadValue() * 0.05f : default);

			Vector2 cameraRotationDelta = new Vector2(-_inputTrackers[_currentSmoothingIndex].SmoothMouseDelta.y, _inputTrackers[_currentSmoothingIndex].SmoothMouseDelta.x);
			_cameraRotation = KCCUtility.GetClampedEulerLookRotation(_cameraRotation, cameraRotationDelta, -90.0f, 90.0f);
			_camera.transform.rotation = Quaternion.Euler(_cameraRotation);

			if (_timeWriter != null)
			{
				double unscaledTime = Time.unscaledTimeAsDouble - _baseUnscaledTime;
				WriteInputValues(_timeWriter, $"{unscaledTime:F6}");
			}

			if (_frameWriter != null)
			{
				int frameCount = Time.frameCount - _baseFrameCount;
				WriteInputValues(_frameWriter, $"{frameCount}");
			}

			void ProcessInput(int inputTracker, Vector2 delta)
			{
				InputTracker tracker = _inputTrackers[inputTracker];
				tracker.Process(Time.frameCount, Time.unscaledDeltaTime, delta);
			}

			void ProcessMonitor(int monitorTracker)
			{
				double pendingDeltaTime = Time.unscaledDeltaTime;

				MonitorTracker tracker = _monitorTrackers[monitorTracker];
				tracker.AccumulateDeltaTime(Time.unscaledDeltaTime);

				while (tracker.Advance() == true)
				{
					if (tracker.Writer == null)
						continue;

					tracker.Writer.Add($"{tracker.MonitorTime:F6}");
					tracker.Writer.Add($"{(Time.unscaledDeltaTime * 1000.0f):F3}");
					tracker.Writer.Add($"{(int)(1.0 / Time.unscaledDeltaTime)}");
					tracker.Writer.Add($"{tracker.RefreshCounter - 1}");
					tracker.Writer.Add($"{tracker.RefreshAlpha:F3}");

					ForEachSmoothing(WriteValue, tracker.Writer, (Func<InputTracker, string>)GetAccumulatedSmoothMouseDeltaX);
					ForEachSmoothing(WriteValue, tracker.Writer, (Func<InputTracker, string>)GetAccumulatedSmoothMouseDeltaY);

					tracker.Writer.Write();
				}

				tracker.UpdateRefreshAlpha();
			}

			void WriteInputValues(StatsWriter writer, string firstValue)
			{
				writer.Add(firstValue);
				writer.Add($"{(Time.unscaledDeltaTime * 1000.0f):F3}");
				writer.Add($"{(int)(1.0 / Time.unscaledDeltaTime)}");

				ForEachSmoothing(WriteValue, writer, (Func<InputTracker, string>)GetSmoothMouseDeltaX);
				ForEachSmoothing(WriteValue, writer, (Func<InputTracker, string>)GetAccumulatedSmoothMouseDeltaX);

				ForEachSmoothing(WriteValue, writer, (Func<InputTracker, string>)GetSmoothMouseDeltaY);
				ForEachSmoothing(WriteValue, writer, (Func<InputTracker, string>)GetAccumulatedSmoothMouseDeltaY);

				ForEachSmoothing(WriteValue, writer, (Func<InputTracker, string>)GetSmoothingFrameCount);

				writer.Write();
			}

			void WriteValue(int smoothingIndex, StatsWriter writer, Func<InputTracker, string> getValue)
			{
				writer.Add(getValue(_inputTrackers[smoothingIndex]));
			}

			string GetSmoothMouseDeltaX           (InputTracker inputTracker) => $"{inputTracker.SmoothMouseDelta.x:F3}";
			string GetSmoothMouseDeltaY           (InputTracker inputTracker) => $"{inputTracker.SmoothMouseDelta.y:F3}";
			string GetAccumulatedSmoothMouseDeltaX(InputTracker inputTracker) => $"{inputTracker.AccumulatedSmoothMouseDelta.x:F3}";
			string GetAccumulatedSmoothMouseDeltaY(InputTracker inputTracker) => $"{inputTracker.AccumulatedSmoothMouseDelta.y:F3}";
			string GetSmoothingFrameCount         (InputTracker inputTracker) => $"{inputTracker.SmoothingFrameCount}";
		}

		private void OnDestroy()
		{
			DeinitializeRecording();
		}

		private void OnGUI()
		{
			InitializeGUI();

			float verticalSpace   = 5.0f;
			float horizontalSpace = 5.0f;

			GUILayout.BeginVertical();
			GUILayout.Space(verticalSpace);
			GUILayout.BeginHorizontal();
			GUILayout.Space(horizontalSpace);

			int      inputSmoothingMs    = GetCurrentInputSmoothing();
			GUIStyle inputSmoothingStyle = inputSmoothingMs <= 0 ? _defaultStyle : (inputSmoothingMs < 35 ? _activeStyle : _inactiveStyle);

			if (GUILayout.Button($"[F4] Input Smoothing ({inputSmoothingMs}ms)", inputSmoothingStyle) == true)
			{
				ToggleInputSmoothing();
			}

			string   frameRate      = Application.targetFrameRate == 0 ? "Unlimited" : Application.targetFrameRate.ToString();
			GUIStyle frameRateStyle = Application.targetFrameRate == 0 ? _defaultStyle : _activeStyle;

			if (GUILayout.Button($"[F5] FPS ({frameRate} / {_frameRateUpdater.SmoothFrameRate})", frameRateStyle) == true)
			{
				ToggleFrameRate();
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

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (GUILayout.Button($"[Enter] Cursor", _defaultStyle) == true)
				{
					ToggleCursor();
				}
			}

			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
		}

		// PRIVATE METHODS

		private void InitializeRecording()
		{
			if (_isInitialized == true)
				return;

			CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

			_baseFrameCount   = Time.frameCount;
			_baseUnscaledTime = Time.unscaledTime;

			if (_frameWriter == null)
			{
				_frameWriter = CreateInputWriter("Frame", "Frame");
			}

			if (_timeWriter == null)
			{
				_timeWriter = CreateInputWriter("EngineTime", "Engine Time [s]");
			}

			ForEachMonitor(InitializeMonitorWriter);

			_isInitialized = true;

			void InitializeMonitorWriter(int monitorIndex)
			{
				MonitorTracker tracker = _monitorTrackers[monitorIndex];
				if (tracker.Writer == null)
				{
					tracker.Writer = CreateMonitorWriter(tracker.RefreshRate, "MonitorTime", "Monitor Time [s]");
				}
			}

			StatsWriter CreateInputWriter(string identifier, string firstHeader)
			{
				string fileID        = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
				string statsFileName = $"Rec_{fileID}_{nameof(InputSmoothing)}_{identifier}.log";

				List<string> headers = new List<string>();
				headers.Add(firstHeader);
				headers.Add($"Engine Delta Time [ms]");
				headers.Add($"Render Speed [FPS]");

				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta X");
				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta X - Accumulated");

				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta Y");
				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta Y - Accumulated");

				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Smoothing Frames");

				StatsWriter writer = new StatsWriter();
				writer.Initialize(statsFileName, GetFilePath(), fileID, headers.ToArray());
				return writer;
			}

			StatsWriter CreateMonitorWriter(int refreshRate, string identifier, string firstHeader)
			{
				string fileID        = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss}";
				string statsFileName = $"Rec_{fileID}_{nameof(InputSmoothing)}_{identifier}_{refreshRate}Hz.log";

				List<string> headers = new List<string>();
				headers.Add(firstHeader);
				headers.Add($"Engine Delta Time [ms]");
				headers.Add($"Render Speed [FPS]");
				headers.Add($"Frame Repeat Counter");
				headers.Add($"Refresh Alpha");

				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta X - Accumulated");
				ForEachSmoothing(AddHeaderWithTimeMs, headers, "[{0}ms] Mouse Delta Y - Accumulated");

				StatsWriter writer = new StatsWriter();
				writer.Initialize(statsFileName, GetFilePath(), fileID, headers.ToArray());
				return writer;
			}

			void AddHeaderWithTimeMs(int smoothingIndex, List<string> headers, string format)
			{
				headers.Add(string.Format(format, _inputSmoothingsMs[smoothingIndex]));
			}
		}

		private void DeinitializeRecording()
		{
			if (_isInitialized == false)
				return;

			if (_timeWriter != null)
			{
				_timeWriter.Deinitialize();
				_timeWriter = null;
			}

			if (_frameWriter != null)
			{
				_frameWriter.Deinitialize();
				_frameWriter = null;
			}

			ForEachMonitor(DeinitializeMonitorWriter);

			void DeinitializeMonitorWriter(int monitorIndex)
			{
				MonitorTracker tracker = _monitorTrackers[monitorIndex];
				if (tracker.Writer != null)
				{
					tracker.Writer.Deinitialize();
					tracker.Writer = null;
				}
			}
		}

		private void InitializeGUI()
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

		private void ProcessKeyboardShortcuts()
		{
			Keyboard keyboard = Keyboard.current;
			if (keyboard == null)
				return;

			float moveSpeed   = 3.0f * Time.deltaTime;
			float rotateSpeed = 90.0f * Time.deltaTime;

			if (keyboard.leftShiftKey.isPressed == true)
			{
				if (keyboard.wKey.isPressed == true)
				{
					_cameraRotation = KCCUtility.GetClampedEulerLookRotation(_cameraRotation, new Vector2(rotateSpeed, 0.0f), -90.0f, 90.0f);
					_camera.transform.rotation = Quaternion.Euler(_cameraRotation);
					_camera.transform.position += _camera.transform.up * moveSpeed;
				}

				if (keyboard.sKey.isPressed == true)
				{
					_cameraRotation = KCCUtility.GetClampedEulerLookRotation(_cameraRotation, new Vector2(-rotateSpeed, 0.0f), -90.0f, 90.0f);
					_camera.transform.rotation = Quaternion.Euler(_cameraRotation);
					_camera.transform.position -= _camera.transform.up * moveSpeed;
				}

				if (keyboard.aKey.isPressed == true)
				{
					_cameraRotation = KCCUtility.GetClampedEulerLookRotation(_cameraRotation, new Vector2(0.0f, rotateSpeed), -90.0f, 90.0f);
					_camera.transform.rotation = Quaternion.Euler(_cameraRotation);
					_camera.transform.position -= _camera.transform.right * moveSpeed;
				}

				if (keyboard.dKey.isPressed == true)
				{
					_cameraRotation = KCCUtility.GetClampedEulerLookRotation(_cameraRotation, new Vector2(0.0f, -rotateSpeed), -90.0f, 90.0f);
					_camera.transform.rotation = Quaternion.Euler(_cameraRotation);
					_camera.transform.position += _camera.transform.right * moveSpeed;
				}
			}
			else
			{
				if (keyboard.wKey.isPressed == true) { _camera.transform.position += _camera.transform.forward * moveSpeed; }
				if (keyboard.sKey.isPressed == true) { _camera.transform.position -= _camera.transform.forward * moveSpeed; }
				if (keyboard.aKey.isPressed == true) { _camera.transform.position -= _camera.transform.right   * moveSpeed; }
				if (keyboard.dKey.isPressed == true) { _camera.transform.position += _camera.transform.right   * moveSpeed; }
				if (keyboard.eKey.isPressed == true) { _camera.transform.position += _camera.transform.up      * moveSpeed; }
				if (keyboard.qKey.isPressed == true) { _camera.transform.position -= _camera.transform.up      * moveSpeed; }
			}

			if (keyboard.f4Key.wasPressedThisFrame == true)
			{
				ToggleInputSmoothing();
			}

			if (keyboard.f5Key.wasPressedThisFrame == true)
			{
				ToggleFrameRate();
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

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				if (keyboard.enterKey.wasPressedThisFrame == true || keyboard.numpadEnterKey.wasPressedThisFrame == true)
				{
					ToggleCursor();
				}
			}
		}

		private void ToggleFrameRate()
		{
			_frameRateUpdater.Toggle();
		}

		private void ToggleVSync()
		{
			QualitySettings.vSyncCount = QualitySettings.vSyncCount == 0 ? 1 : 0;
		}

		private int GetCurrentInputSmoothing()
		{
			return _inputSmoothingsMs[_currentSmoothingIndex];
		}

		private void ToggleInputSmoothing()
		{
			_currentSmoothingIndex = (_currentSmoothingIndex + 1) % _inputSmoothingsMs.Length;
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
			_enableRecording = !_enableRecording;
		}

		private string GetFilePath()
		{
			string filePath = default;
#if UNITY_EDITOR
			filePath = UnityEditor.AssetDatabase.GetAssetPath(UnityEditor.MonoScript.FromMonoBehaviour(this));
			filePath = System.IO.Path.GetDirectoryName(filePath);
#endif
			return filePath;
		}

		private void ForEachSmoothing            (Action<int>             callback)                            { for (int i = 0; i < _inputSmoothingsMs.Length; ++i) { callback(i);                   } }
		private void ForEachSmoothing<T1>        (Action<int, T1>         callback, T1 arg1)                   { for (int i = 0; i < _inputSmoothingsMs.Length; ++i) { callback(i, arg1);             } }
		private void ForEachSmoothing<T1, T2>    (Action<int, T1, T2>     callback, T1 arg1, T2 arg2)          { for (int i = 0; i < _inputSmoothingsMs.Length; ++i) { callback(i, arg1, arg2);       } }
		private void ForEachSmoothing<T1, T2, T3>(Action<int, T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3) { for (int i = 0; i < _inputSmoothingsMs.Length; ++i) { callback(i, arg1, arg2, arg3); } }

		private void ForEachMonitor            (Action<int>             callback)                            { for (int i = 0; i < _monitorRefreshRates.Length; ++i) { callback(i);                   } }
		private void ForEachMonitor<T1>        (Action<int, T1>         callback, T1 arg1)                   { for (int i = 0; i < _monitorRefreshRates.Length; ++i) { callback(i, arg1);             } }
		private void ForEachMonitor<T1, T2>    (Action<int, T1, T2>     callback, T1 arg1, T2 arg2)          { for (int i = 0; i < _monitorRefreshRates.Length; ++i) { callback(i, arg1, arg2);       } }
		private void ForEachMonitor<T1, T2, T3>(Action<int, T1, T2, T3> callback, T1 arg1, T2 arg2, T3 arg3) { for (int i = 0; i < _monitorRefreshRates.Length; ++i) { callback(i, arg1, arg2, arg3); } }

		// DATA STRUCTURES

		private sealed class InputTracker
		{
			public Vector2 MouseDelta                  => _mouseDelta;
			public Vector2 SmoothMouseDelta            => _smoothMouseDelta;
			public Vector2 AccumulatedMouseDelta       => _accumulatedMouseDelta;
			public Vector2 AccumulatedSmoothMouseDelta => _accumulatedSmoothMouseDelta;
			public int     SmoothingFrameCount         => _smoothingFrameCount;

			private Vector2       _mouseDelta;
			private Vector2       _smoothMouseDelta;
			private Vector2       _accumulatedMouseDelta;
			private Vector2       _accumulatedSmoothMouseDelta;
			private int           _smoothingFrameCount;
			private SmoothVector2 _mouseDeltaValues;
			private float         _responsivity;

			public InputTracker(int samples, float responsivity)
			{
				_mouseDeltaValues = new SmoothVector2(samples);
				_responsivity     = responsivity;
			}

			public void Process(int frame, float unscaledDeltaTime, Vector2 mouseDelta)
			{
				_mouseDeltaValues.AddValue(frame, unscaledDeltaTime, mouseDelta);

				_mouseDelta                   = mouseDelta;
				_smoothMouseDelta             = _mouseDeltaValues.CalculateSmoothValue(_responsivity, unscaledDeltaTime, out _smoothingFrameCount);
				_accumulatedMouseDelta       += mouseDelta;
				_accumulatedSmoothMouseDelta += _smoothMouseDelta;
			}
		}

		private sealed class MonitorTracker
		{
			public StatsWriter Writer;

			public int    RefreshRate    => _refreshRate;
			public int    RefreshCounter => _refreshCounter;
			public double RefreshAlpha   => _refreshAlpha;
			public double MonitorTime    => _monitorTime;

			private int    _refreshRate;
			private int    _refreshCounter;
			private double _refreshDeltaTime;
			private double _refreshAlpha;
			private double _pendingDeltaTime;
			private double _monitorTime;

			public MonitorTracker(int refreshRate)
			{
				_refreshRate      = refreshRate;
				_refreshDeltaTime = 1.0 / refreshRate;
			}

			public void AccumulateDeltaTime(double deltaTime)
			{
				_refreshCounter = default;
				_pendingDeltaTime += deltaTime;
			}

			public bool Advance()
			{
				if (_pendingDeltaTime < _refreshDeltaTime)
					return false;

				++_refreshCounter;
				_pendingDeltaTime -= _refreshDeltaTime;
				_monitorTime += _refreshDeltaTime;

				return true;
			}

			public void UpdateRefreshAlpha()
			{
				_refreshAlpha = _pendingDeltaTime / _refreshDeltaTime;
			}
		}
	}
}
