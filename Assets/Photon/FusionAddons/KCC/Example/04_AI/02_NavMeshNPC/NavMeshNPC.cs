namespace Example.NavMeshNPC
{
	using UnityEngine;
	using UnityEngine.AI;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example of NPC - travelling between waypoints based on NavMesh.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	[RequireComponent(typeof(KCC))]
	public sealed class NavMeshNPC : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform[] _waypoints;

		private KCC         _kcc;
		private Transform   _transform;
		private NavMeshPath _navMeshPath;
		private int         _currentNavMeshCorner;
		private int         _currentWaypoint;
		private Vector3     _checkPosition;
		private int         _checkTicks;

		// NetworkBehaviour INTERFACE

		public override void FixedUpdateNetwork()
		{
			CheckPosition();
			SetDirection();
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_kcc       = gameObject.GetComponent<KCC>();
			_transform = gameObject.GetComponent<Transform>();
		}

		// PRIVATE METHODS

		private void CheckPosition()
		{
			Vector3 position = _transform.position;
			if (position.AlmostEquals(_checkPosition, 0.5f) == true)
			{
				++_checkTicks;
				if (_checkTicks > 100)
				{
					_navMeshPath   = default;
					_checkPosition = default;
					_checkTicks    = default;
				}
			}
			else
			{
				_checkPosition = position;
				_checkTicks    = default;
			}
		}

		private void SetDirection()
		{
			if (_waypoints.Length == 0)
				return;

			// Calcualte nav mesh path for the first time.
			if (_navMeshPath == null)
			{
				_navMeshPath = new NavMeshPath();
				_currentWaypoint = Random.Range(0, _waypoints.Length);
				_currentNavMeshCorner = 0;
				NavMesh.CalculatePath(transform.position, _waypoints[_currentWaypoint].position, -1, _navMeshPath);
			}

			// Distance check against current waypoint. The waypoint must be on navmesh.
			Vector3 waypointDirection = (_waypoints[_currentWaypoint].position - _kcc.Data.TargetPosition).OnlyXZ();
			if (waypointDirection.sqrMagnitude < 1.0f)
			{
				int randomWaypoint = Random.Range(0, _waypoints.Length);
				if (randomWaypoint != _currentWaypoint)
				{
					_currentWaypoint = randomWaypoint;
					_currentNavMeshCorner = 0;
					NavMesh.CalculatePath(_kcc.transform.position, _waypoints[_currentWaypoint].position, -1, _navMeshPath);

					_kcc.Jump((Vector3.up + waypointDirection) * 5.0f);
				}
			}

			if (_navMeshPath.corners.Length <= _currentNavMeshCorner)
			{
				// Oops, path not found. This needs to be handled - recalculate path, find new target waypoint, ...
				_kcc.SetInputDirection(Vector3.zero);
				return;
			}

			Vector3 cornerDirection = (_navMeshPath.corners[_currentNavMeshCorner] - _kcc.Data.TargetPosition).OnlyXZ();
			if (cornerDirection.sqrMagnitude < 0.25f)
			{
				// NavMesh corner reached, go to next.
				++_currentNavMeshCorner;
			}

			// Setting KCC properties, these calls will be ignored on proxies.

			Quaternion lookRotation = cornerDirection.IsAlmostZero() ? Quaternion.identity : Quaternion.LookRotation(cornerDirection);

			_kcc.SetLookRotation(lookRotation);
			_kcc.SetInputDirection(cornerDirection);
		}
	}
}
