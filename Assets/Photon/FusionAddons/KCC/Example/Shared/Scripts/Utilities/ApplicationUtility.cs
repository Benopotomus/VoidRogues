namespace Example
{
	using System;
	using UnityEngine;

	public static class ApplicationUtility
	{
		// PUBLIC METHODS

		public static bool IsVREnabled()
		{
			return false;

			//return XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null && XRGeneralSettings.Instance.Manager.activeLoader != null;
		}

		public static bool HasCommandLineArgument(string name)
		{
			string[] arguments = Environment.GetCommandLineArgs();
			for (int i = 0; i < arguments.Length; ++i)
			{
				if (arguments[i] == name)
					return true;
			}

			return false;
		}

		public static bool GetCommandLineArgument(string name, out string argument)
		{
			string[] arguments = Environment.GetCommandLineArgs();
			for (int i = 0; i < arguments.Length; ++i)
			{
				if (arguments[i] == name && arguments.Length > (i + 1))
				{
					argument = arguments[i + 1];
					return true;
				}
			}

			argument = default;
			return false;
		}

		public static bool GetCommandLineArgument(string name, out int argument)
		{
			string[] arguments = Environment.GetCommandLineArgs();
			for (int i = 0; i < arguments.Length; ++i)
			{
				if (arguments[i] == name && arguments.Length > (i + 1) && int.TryParse(arguments[i + 1], out int parsedArgument) == true)
				{
					argument = parsedArgument;
					return true;
				}
			}

			argument = default;
			return false;
		}

		public static int GetRefreshRate(Resolution resolution)
		{
#if UNITY_2022_3_OR_NEWER
			return Mathf.RoundToInt((float)resolution.refreshRateRatio.value);
#else
			return resolution.refreshRate;
#endif
		}

		public static void SetResolution(int width, int height, FullScreenMode fullscreenMode, int preferredRefreshRate)
		{
#if UNITY_2022_3_OR_NEWER
			Screen.SetResolution(width, height, fullscreenMode, new RefreshRate() { numerator = (uint)preferredRefreshRate, denominator = 1U });
#else
			Screen.SetResolution(width, height, fullscreenMode, preferredRefreshRate);
#endif
		}
	}
}
