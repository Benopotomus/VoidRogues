namespace Example
{
	using System.Collections.Generic;
	using UnityEngine;
	using Fusion.Addons.KCC;

	public sealed class FrameRateUpdater : MonoBehaviour
	{
		// PUBLIC MEMBERS

		public int TargetFrameRate => _targetFrameRate;
		public int SmoothFrameRate => _smoothFrameRate;

		// PRIVATE MEMBERS

		private int       _targetFrameRate;
		private List<int> _defaultFrameRates = new List<int>();
		private List<int> _mobileFrameRates  = new List<int>();
		private float[]   _deltaTimes        = new float[100];
		private int       _deltaTimeIndex;
		private int       _smoothFrameRate;

		// PUBLIC METHODS

		public void Toggle()
		{
			List<int> frameRates = _defaultFrameRates;

			if (Application.isMobilePlatform == true && Application.isEditor == false)
			{
				frameRates = _mobileFrameRates;
			}

			int index = frameRates.IndexOf(_targetFrameRate);
			index = index >= 0 ? (index + 1) % frameRates.Count : 0;

			_targetFrameRate = frameRates[index];

			Application.targetFrameRate = _targetFrameRate;
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_defaultFrameRates.Add(0);
			_defaultFrameRates.Add(5);
			_defaultFrameRates.Add(15);
			_defaultFrameRates.Add(30);
			_defaultFrameRates.Add(60);
			_defaultFrameRates.Add(90);
			_defaultFrameRates.Add(120);
			_defaultFrameRates.Add(144);
			_defaultFrameRates.Add(150);
			_defaultFrameRates.Add(165);
			_defaultFrameRates.Add(200);
			_defaultFrameRates.Add(240);
			_defaultFrameRates.Add(288);
			_defaultFrameRates.Add(325);
			_defaultFrameRates.Add(360);
			_defaultFrameRates.Add(390);
			_defaultFrameRates.Add(432);
			_defaultFrameRates.Add(480);
			_defaultFrameRates.Add(576);
			_defaultFrameRates.Add(650);
			_defaultFrameRates.Add(720);
			_defaultFrameRates.Add(850);
			_defaultFrameRates.Add(990);

			_mobileFrameRates.Add(0);
			_mobileFrameRates.Add(5);
			_mobileFrameRates.Add(15);
			_mobileFrameRates.Add(30);
			_mobileFrameRates.Add(60);
			_mobileFrameRates.Add(72);
			_mobileFrameRates.Add(90);
			_mobileFrameRates.Add(120);
			_mobileFrameRates.Add(125);
			_mobileFrameRates.Add(144);
			_mobileFrameRates.Add(165);
			_mobileFrameRates.Add(200);

			int currentRefreshRate = ApplicationUtility.GetRefreshRate(Screen.currentResolution);
			if (currentRefreshRate > 0)
			{
				_defaultFrameRates.AddUnique(currentRefreshRate);
				_defaultFrameRates.AddUnique(currentRefreshRate * 2);
				_defaultFrameRates.AddUnique(currentRefreshRate * 3);
				_defaultFrameRates.AddUnique(currentRefreshRate * 4);

				_mobileFrameRates.AddUnique(currentRefreshRate);
				_mobileFrameRates.AddUnique(currentRefreshRate * 2);
				_mobileFrameRates.AddUnique(currentRefreshRate * 3);
				_mobileFrameRates.AddUnique(currentRefreshRate * 4);
			}

			_targetFrameRate = Mathf.Max(currentRefreshRate, 0);

			if (ApplicationUtility.GetCommandLineArgument("-targetFrameRate", out int targetFrameRate) == true)
			{
				_targetFrameRate = targetFrameRate;
			}

			Application.targetFrameRate = _targetFrameRate;
		}

		private void Update()
		{
			Application.targetFrameRate = _targetFrameRate;

			_deltaTimeIndex = (_deltaTimeIndex + 1) % _deltaTimes.Length;
			_deltaTimes[_deltaTimeIndex] = Time.unscaledDeltaTime;

			float totalDeltaTime = 0.0f;
			for (int i = 0; i < _deltaTimes.Length; ++i)
			{
				totalDeltaTime += _deltaTimes[i];
			}
			totalDeltaTime /= _deltaTimes.Length;

			_smoothFrameRate = totalDeltaTime > 0.000001f ? Mathf.RoundToInt(1.0f / totalDeltaTime) : 0;
		}
	}
}
