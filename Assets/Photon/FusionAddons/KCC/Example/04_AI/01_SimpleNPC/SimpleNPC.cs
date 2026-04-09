namespace Example.SimpleNPC
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example of simple NPC - travelling between waypoints.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	[RequireComponent(typeof(KCC))]
	public sealed class SimpleNPC : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform[] _waypoints;

		private KCC _kcc;
		private int _currentWaypoint;

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			SetDirection();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_kcc = gameObject.GetComponent<KCC>();
		}

		// PRIVATE METHODS

		private void SetDirection()
		{
			if (_waypoints.Length == 0)
				return;

			// Distance check against current waypoint.

			Vector3 direction = (_waypoints[_currentWaypoint].position - _kcc.Data.TargetPosition).OnlyXZ();
			if (direction.sqrMagnitude < 1.0f)
			{
				_currentWaypoint = (_currentWaypoint + 1) % _waypoints.Length;

				_kcc.Jump((Vector3.up + direction) * 5.0f);
			}

			// Setting KCC properties, these calls will be ignored on proxies.

			_kcc.SetLookRotation(Quaternion.LookRotation(direction));
			_kcc.SetInputDirection(direction);
		}
	}
}
