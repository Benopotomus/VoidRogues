namespace Example
{
	using UnityEngine;
	using UnityEngine.SceneManagement;

	/// <summary>
	/// Script for handling command line arguments.
	/// </summary>
	public sealed class Startup : MonoBehaviour
	{
		// PRIVATE MEMBERS

		private static bool _isInitialized;

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			if (_isInitialized == true)
				return;

			_isInitialized = true;

			if (ApplicationUtility.GetCommandLineArgument("-scene", out string scene) == true)
			{
				SceneManager.LoadScene(scene);
			}
		}
	}
}
