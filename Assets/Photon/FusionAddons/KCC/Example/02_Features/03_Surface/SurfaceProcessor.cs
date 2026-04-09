namespace Example.Surface
{
	using System.Collections.Generic;
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	#if UNITY_6000_0_OR_NEWER
	// Compatibility with lower Unity versions.
	using PhysicMaterial = UnityEngine.PhysicsMaterial;
	#endif

	/// <summary>
	/// Example processor - multiplying kinematic speed of the KCC + applying friction based on physics materials.
	/// This processor also implements IMapStatusProvider - providing status text about active slowdown shown in UI.
	/// </summary>
	public sealed class SurfaceProcessor : KCCProcessor, ISetKinematicSpeed, IMapStatusProvider
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private int _priority;
		[SerializeField]
		private List<PhysicMaterial> _physicMaterials;

		private Dictionary<PlayerRef, float>          _activeFrictions = new Dictionary<PlayerRef, float>();
		private Dictionary<PlayerRef, PhysicMaterial> _activeMaterials = new Dictionary<PlayerRef, PhysicMaterial>();

		// KCCProcessor INTERFACE

		public override float GetPriority(KCC kcc) => _priority;

		// ISetKinematicSpeed INTERFACE

		public void Execute(ISetKinematicSpeed stage, KCC kcc, KCCData data)
		{
			float          highestFriction         = default;
			PhysicMaterial highestFrictionMaterial = default;

			if (_physicMaterials.Count > 0)
			{
				// Iterate over all collider hits and get highest friction from all physics materials this processor reacts to.
				foreach (KCCHit hit in data.Hits.All)
				{
					// Check if the collider is still valid since last frame.
					if (hit.Collider == null)
						continue;

					PhysicMaterial physicMaterial = hit.Collider.sharedMaterial;
					if (physicMaterial != null)
					{
						foreach (PhysicMaterial validPhysicMaterial in _physicMaterials)
						{
							if (physicMaterial == validPhysicMaterial)
							{
								float friction = data.RealSpeed > 0.001f ? physicMaterial.dynamicFriction : physicMaterial.staticFriction;
								if (friction > highestFriction)
								{
									highestFriction         = friction;
									highestFrictionMaterial = physicMaterial;

									if (highestFriction >= 1.0f)
									{
										highestFriction = 1.0f;
										break;
									}
								}
							}
						}

						if (highestFriction >= 1.0f)
						{
							highestFriction = 1.0f;
							break;
						}
					}
				}
			}

			// Storing info in non-networked variables is generally not safe here because of prediction/resimulations.
			// In this case it is OK, we use the data only for presentation in UI which doesn't affect gameplay.
			_activeFrictions[kcc.Object.InputAuthority] = highestFriction;
			_activeMaterials[kcc.Object.InputAuthority] = highestFrictionMaterial;

			// Apply friction.
			data.KinematicSpeed *= 1.0f - highestFriction;
		}

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return _activeFrictions.TryGetValue(player, out float friction) == true && friction > 0.0f;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			if (_activeFrictions.TryGetValue(player, out float friction) == false || friction <= 0.0f)
				return "";
			if (_activeMaterials.TryGetValue(player, out PhysicMaterial material) == false || material == null)
				return "";

			// Status text is simple combination of physics material name and friction converted to percentual slowdown.
			return $"{material.name} - {Mathf.RoundToInt(friction * 100.0f)}% slowdown";
		}
	}
}
