namespace Example.ExpertMovement
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Component maintaining player abilities - spawning, parenting, ...
	/// </summary>
	[DefaultExecutionOrder(-15)]
	public sealed class ExpertPlayerAbilities : NetworkBehaviour
	{
		// CONSTANTS

		private const int MAX_ABILITIES = 8;

		// PRIVATE MEMBERS

		[Networked][Capacity(MAX_ABILITIES)]
		private NetworkArray<NetworkObject> _networkedAbilities { get; }

		private NetworkObject[] _localAbilities = new NetworkObject[MAX_ABILITIES];

		// PUBLIC METHODS

		public bool TryGetAbility<T>(out T ability) where T : class
		{
			// This method iterates over LOCAL array of abilities to ensure parenting is already done and the ability is at correct position in hierarchy.
			for (int i = 0, count = _localAbilities.Length; i < count; ++i)
			{
				NetworkObject localAbility = _localAbilities[i];
				if (localAbility != null && localAbility.TryGetComponent<T>(out T component) == true)
				{
					ability = component;
					return true;
				}
			}

			ability = default;
			return default;
		}

		public NetworkObject AddAbility(NetworkObject abilityPrefab)
		{
			return AddAbility(abilityPrefab, true);
		}

		public bool RemoveAbility(NetworkObject ability)
		{
			if (HasStateAuthority == false)
				return default;

			for (int i = 0; i < _localAbilities.Length; ++i)
			{
				if (_localAbilities[i] == ability)
				{
					_localAbilities[i] = default;
					break;
				}
			}

			for (int i = 0; i < _networkedAbilities.Length; ++i)
			{
				NetworkObject networkedAbility = _networkedAbilities.Get(i);
				if (networkedAbility == ability)
				{
					_networkedAbilities.Set(i, default);
					networkedAbility.transform.SetParent(null);
					Runner.Despawn(networkedAbility);
					return true;
				}
			}

			return default;
		}

		public void AddAbilities(NetworkObject[] abilityPrefabs)
		{
			bool synchronizeLocalAbilities = false;

			for (int i = 0; i < abilityPrefabs.Length; ++i)
			{
				synchronizeLocalAbilities |= AddAbility(abilityPrefabs[i], false) != null;
			}

			if (synchronizeLocalAbilities == true)
			{
				SynchronizeLocalAbilities();
			}
		}

		public void RemoveAbilities()
		{
			if (HasStateAuthority == false)
				return;

			ClearLocalAbilities();

			for (int i = 0, count = _networkedAbilities.Length; i < count; ++i)
			{
				NetworkObject networkedAbility = _networkedAbilities[i];
				if (networkedAbility != null)
				{
					Runner.Despawn(networkedAbility);
				}

				_networkedAbilities.Set(i, null);
			}
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			_localAbilities.Clear();

			if (HasStateAuthority == true)
			{
				_networkedAbilities.Clear();
			}
			else
			{
				SynchronizeLocalAbilities();
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			ClearLocalAbilities();
			RemoveAbilities();
		}

		public override void FixedUpdateNetwork()
		{
			SynchronizeLocalAbilities();
		}

		// MonoBehaviour INTERFACE

		private void Update()
		{
			if (IsProxy == false)
				return;

			// Proxy don't execute FixedUpdateNetwork(), we have to synchronize abilities from Update().
			SynchronizeLocalAbilities();
		}

		// PRIVATE METHODS

		private NetworkObject AddAbility(NetworkObject abilityPrefab, bool synchronizeLocalAbilities)
		{
			if (abilityPrefab == null)
				return default;

			if (HasStateAuthority == false)
				return default;

			for (int i = 0; i < _networkedAbilities.Length; ++i)
			{
				NetworkObject networkedAbility = _networkedAbilities.Get(i);
				if (networkedAbility != null)
					continue;

				networkedAbility = Runner.Spawn(abilityPrefab, transform.position, null, Object.InputAuthority, SetParentTransform);
				_networkedAbilities.Set(i, networkedAbility);

				if (synchronizeLocalAbilities == true)
				{
					SynchronizeLocalAbilities();
				}

				return networkedAbility;
			}

			return default;
		}

		private void SynchronizeLocalAbilities()
		{
			// This method synchronizes networked list of abilities with a local list.
			// This approach is robust and ensures correct initialization on all peers - server / host / client in all modes.

			for (int i = 0, count = _networkedAbilities.Length; i < count; ++i)
			{
				NetworkObject networkedAbility = _networkedAbilities[i];
				NetworkObject localAbility     = _localAbilities[i];

				if (localAbility == networkedAbility)
					continue;

				if (localAbility != null)
				{
					localAbility.transform.SetParent(null);
				}

				localAbility = networkedAbility;

				localAbility.transform.SetParent(transform);
				localAbility.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

				_localAbilities[i] = localAbility;
			}
		}

		private void ClearLocalAbilities()
		{
			for (int i = 0, count = _localAbilities.Length; i < count; ++i)
			{
				NetworkObject localAbility = _localAbilities[i];
				if (localAbility != null && localAbility.IsValid == true)
				{
					localAbility.transform.SetParent(null);
				}

				_localAbilities[i] = null;
			}
		}

		private void SetParentTransform(NetworkRunner runner, NetworkObject networkObject)
		{
			networkObject.transform.SetParent(transform);
		}
	}
}
