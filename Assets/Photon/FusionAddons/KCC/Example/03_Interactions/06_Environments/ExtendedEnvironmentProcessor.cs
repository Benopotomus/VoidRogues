namespace Example.Environments
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - derived from base EnvironmentProcessor.
	/// This processor also implements IMapStatusProvider - providing status text about active effect to be shown in UI.
	/// </summary>
	public sealed class ExtendedEnvironmentProcessor : EnvironmentProcessor, IMapStatusProvider
	{
		// PUBLIC MEMBERS

		[Header("Custom")]
		[Tooltip("Status text shown in UI.")]
		public string Description;

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return true;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			return $"{name} - {Description}";
		}
	}
}
