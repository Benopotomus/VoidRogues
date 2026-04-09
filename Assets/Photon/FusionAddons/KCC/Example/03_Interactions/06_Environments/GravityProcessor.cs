namespace Example.Environments
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - setting custom gravity (absolute value or multiplier).
	/// This processor also implements IMapStatusProvider - providing status text about active gravity effect to be shown in UI.
	/// </summary>
	public sealed class GravityProcessor : KCCProcessor, ISetGravity, IMapStatusProvider
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _gravity = 1.0f;
		[SerializeField]
		private bool  _isMultiplier = true;

		// ISetGravity INTERFACE

		public void Execute(ISetGravity stage, KCC kcc, KCCData data)
		{
			if (_isMultiplier == true)
			{
				data.Gravity *= _gravity;
			}
			else
			{
				data.Gravity = new Vector3(0.0f, _gravity, 0.0f);
			}

			// Suppress other ISetGravity processors with lower priority.
			kcc.SuppressProcessors<ISetGravity>();
		}

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return true;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			if (_isMultiplier == true)
			{
				return $"{name} - {Mathf.RoundToInt(_gravity * 100.0f)}% Gravity";
			}
			else
			{
				return $"{name} - Gravity {_gravity}";
			}
		}
	}
}
