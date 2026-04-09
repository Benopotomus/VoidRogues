namespace Example
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Script for setting photon cap visual based on network authority state.
	/// </summary>
	public sealed class PhotonCap : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Material _inputAuthority;
		[SerializeField]
		private Material _stateAuthority;
		[SerializeField]
		private Material _proxy;
		[SerializeField]
		private Renderer _renderer;

		private int _currentMaterialId = -1;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			RefreshMaterial();
		}

		public override void Render()
		{
			RefreshMaterial();
		}

		// PRIVATE METHODS

		private void RefreshMaterial()
		{
			if (HasInputAuthority == true)
			{
				if (_currentMaterialId != 0)
				{
					SetMaterial(0, _inputAuthority);
				}
			}
			else if (HasStateAuthority == true)
			{
				if (_currentMaterialId != 1)
				{
					SetMaterial(1, _stateAuthority);
				}
			}
			else
			{
				if (_currentMaterialId != 2)
				{
					SetMaterial(2, _proxy);
				}
			}
		}

		private void SetMaterial(int materialId, Material material)
		{
			_currentMaterialId = materialId;
			_renderer.material = material;
		}
	}
}
