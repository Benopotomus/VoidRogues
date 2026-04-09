namespace Example.MovingPlatform
{
	using System;
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Basic platform which moves the object between waypoints. It must be executed first, before any player executes its movement.
	/// This script needs to be a KCC processor (deriving from NetworkKCCProcessor) to be correctly tracked by PlatformProcessor.
	/// It also implements IMapStatusProvider - providing status text about waiting/travel time shown in UI.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
    public sealed unsafe class MovingPlatform : NetworkKCCProcessor, IPlatform, IMapStatusProvider
    {
		// PRIVATE MEMBERS

		[SerializeField]
		private EPlatformMode _mode;
		[SerializeField]
		private float _speed = 1.0f;
		[SerializeField]
		private PlatformWaypoint[] _waypoints;

		[Networked]
		private int _baseTick { get; set; }

		private Transform _transform;
		private Rigidbody _rigidbody;
		private float     _cycleTime;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			_cycleTime = default;

			if (HasStateAuthority == true)
			{
				_baseTick = Runner.Tick;
			}
			else
			{
				SetTransform(Runner.Tick);
			}

			// By default FixedUpdateNetwork() is not executed on proxies.
			// We register callbacks to ensure the platform is predicted everywhere.
			// The platform is updated first because PlatformManager has lower execution order.
			PlatformManager manager = Runner.GetSingleton<PlatformManager>();
			manager.OnFixedUpdateNetwork += OnFixedUpdateNetwork;
			manager.OnRender += OnRender;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			PlatformManager manager = runner.GetSingleton<PlatformManager>();
			manager.OnFixedUpdateNetwork -= OnFixedUpdateNetwork;
			manager.OnRender -= OnRender;
		}

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return true;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			float tick = Runner.Tick + Runner.LocalAlpha;
			float time = (tick - _baseTick) * Runner.DeltaTime;

			CalculateTransform(time, out Vector3 position, out Quaternion rotation, out int fromWaypointIndex, out int toWaypointIndex, out float waypointsAlpha, out float waitTime);

			string fromWaypointName = fromWaypointIndex >= 0 && fromWaypointIndex < _waypoints.Length ? _waypoints[fromWaypointIndex].Name : "---";
			string toWaypointName   = toWaypointIndex   >= 0 && toWaypointIndex   < _waypoints.Length ? _waypoints[toWaypointIndex].Name   : "---";
			string waypointName     = waypointsAlpha < 0.5f ? fromWaypointName : toWaypointName;

			if (waitTime > 0.0f)
				return $"{waypointName} - Waiting {waitTime:F1}s";

			return $"{fromWaypointName} -> {toWaypointName} ({Mathf.RoundToInt(waypointsAlpha * 100.0f)}%)";
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_transform = transform;
			_rigidbody = GetComponent<Rigidbody>();

			if (_rigidbody == null)
				throw new NullReferenceException($"GameObject {name} has missing Rigidbody component!");

			_rigidbody.isKinematic   = true;
			_rigidbody.useGravity    = false;
			_rigidbody.interpolation = RigidbodyInterpolation.None;
			_rigidbody.constraints   = RigidbodyConstraints.FreezeAll;
		}

		// PRIVATE METHODS

		private void OnFixedUpdateNetwork()
		{
			SetTransform(Runner.Tick);
		}

		private void OnRender()
		{
			SetTransform(Runner.Tick + Runner.LocalAlpha);
		}

		private void SetTransform(float tick)
		{
			float time = (tick - _baseTick) * Runner.DeltaTime;

			CalculateTransform(time, out Vector3 position, out Quaternion rotation, out int fromWaypointIndex, out int toWaypointIndex, out float waypointsAlpha, out float waitTime);

			_transform.SetPositionAndRotation(position, rotation);

			_rigidbody.position = position;
			_rigidbody.rotation = rotation;
		}

		private void CalculateTransform(float time, out Vector3 position, out Quaternion rotation, out int fromWaypointIndex, out int toWaypointIndex, out float waypointsAlpha, out float waitTime)
		{
			fromWaypointIndex = -1;
			toWaypointIndex   = -1;
			waypointsAlpha    = 0.0f;
			waitTime          = 0.0f;

			if (_waypoints.Length <= 0)
			{
				_transform.GetPositionAndRotation(out position, out rotation);
				return;
			}

			if (time <= 0.0f || _waypoints.Length <= 1)
			{
				fromWaypointIndex = 0;
				_waypoints[fromWaypointIndex].GetPositionAndRotation(out position, out rotation);
				return;
			}

			float cycleTime = GetCycleTime();
			if (cycleTime <= 0.0f)
			{
				fromWaypointIndex = 0;
				_waypoints[fromWaypointIndex].GetPositionAndRotation(out position, out rotation);
				return;
			}

			if (_mode == EPlatformMode.None && time >= cycleTime)
			{
				waypointsAlpha    = 1.0f;
				toWaypointIndex   = _waypoints.Length - 1;
				fromWaypointIndex = toWaypointIndex - 1;
				_waypoints[toWaypointIndex].GetPositionAndRotation(out position, out rotation);
				return;
			}

			time %= cycleTime;

			int moveDirection = 1;

			fromWaypointIndex = 0;
			toWaypointIndex   = fromWaypointIndex + moveDirection;

			for (;;)
			{
				PlatformWaypoint fromWaypoint = _waypoints[fromWaypointIndex];

				waypointsAlpha = 0.0f;
				position = fromWaypoint.Position;
				rotation = fromWaypoint.Rotation;

				if (fromWaypoint.ExitWaitTime > time)
				{
					waitTime = fromWaypoint.ExitWaitTime - time;
					return;
				}

				time -= fromWaypoint.ExitWaitTime;

				PlatformWaypoint toWaypoint = _waypoints[toWaypointIndex];

				float fromSpeed = fromWaypoint.GetSpeed(_speed);
				float toSpeed   = toWaypoint.GetSpeed(_speed);
				float speed     = (fromSpeed + toSpeed) * 0.5f;

				float sectionTime = Vector3.Distance(fromWaypoint.Position, toWaypoint.Position) / speed;
				if (sectionTime > time)
				{
					waypointsAlpha = time / sectionTime;
					position = Vector3.Lerp(fromWaypoint.Position, toWaypoint.Position, waypointsAlpha);
					rotation = Quaternion.Slerp(fromWaypoint.Rotation, toWaypoint.Rotation, waypointsAlpha);
					return;
				}
				else
				{
					waypointsAlpha = 1.0f;
					position = toWaypoint.Position;
					rotation = toWaypoint.Rotation;
				}

				time -= sectionTime;

				if (toWaypoint.EnterWaitTime > time)
				{
					waitTime = toWaypoint.EnterWaitTime - time;
					return;
				}

				time -= toWaypoint.EnterWaitTime;

				if (AdvanceWaypoints(ref fromWaypointIndex, ref toWaypointIndex, ref moveDirection) == false)
					return;
			}
		}

		private float GetCycleTime()
		{
			if (_cycleTime > 0.0f)
				return _cycleTime;
			if (_waypoints.Length <= 1)
				return default;

			int   fromWaypointIndex = 0;
			int   toWaypointIndex   = 1;
			int   moveDirection     = 1;
			float cycleTime         = 0.0f;

			for (;;)
			{
				PlatformWaypoint fromWaypoint = _waypoints[fromWaypointIndex];
				PlatformWaypoint toWaypoint   = _waypoints[toWaypointIndex];

				float fromSpeed = fromWaypoint.GetSpeed(_speed);
				float toSpeed   = toWaypoint.GetSpeed(_speed);
				float speed     = (fromSpeed + toSpeed) * 0.5f;

				cycleTime += fromWaypoint.ExitWaitTime;
				cycleTime += Vector3.Distance(fromWaypoint.Position, toWaypoint.Position) / speed;
				cycleTime += toWaypoint.EnterWaitTime;

				if (AdvanceWaypoints(ref fromWaypointIndex, ref toWaypointIndex, ref moveDirection) == false)
					break;
				if (fromWaypointIndex == 0)
					break;
			}

			_cycleTime = cycleTime;

			return cycleTime;
		}

		private bool AdvanceWaypoints(ref int fromIndex, ref int toIndex, ref int direction)
		{
			if (direction == 0)
				return false;

			if (_mode == EPlatformMode.None)
			{
				int nextIndex = toIndex + direction;
				if (nextIndex < 0 || nextIndex >= _waypoints.Length)
					return false;

				fromIndex = toIndex;
				toIndex   = nextIndex;

				return true;
			}
			else if (_mode == EPlatformMode.Looping)
			{
				int nextIndex = toIndex + direction;
				if (nextIndex < 0 || nextIndex >= _waypoints.Length)
				{
					toIndex   = direction > 0 ? 0 : (_waypoints.Length - 1);
					nextIndex = toIndex + direction;
				}

				fromIndex = toIndex;
				toIndex   = nextIndex;

				return true;
			}
			else if (_mode == EPlatformMode.PingPong)
			{
				fromIndex = toIndex;
				toIndex   = fromIndex + direction;

				if (direction > 0)
				{
					if (toIndex >= _waypoints.Length)
					{
						toIndex = _waypoints.Length - (toIndex - _waypoints.Length) - 2;
						direction = -direction;
					}
				}
				else
				{
					if (toIndex < 0)
					{
						toIndex   = -toIndex;
						direction = -direction;
					}
				}

				return true;
			}
			else
			{
				throw new NotImplementedException(_mode.ToString());
			}
		}

		// DATA STRUCTURES

		[Serializable]
		private sealed class PlatformWaypoint
		{
			public float SpeedOverride;
			public float SpeedMultiplier;
			public float EnterWaitTime;
			public float ExitWaitTime;

			[SerializeField]
			public Transform _transform;

			private bool       _isCached;
			private Vector3    _cachedPosition;
			private Quaternion _cachedRotation;

			public string     Name => _transform.name;
			public Vector3    Position { get { if (_isCached == false) { _transform.GetPositionAndRotation(out _cachedPosition, out _cachedRotation); _isCached = true; } return _cachedPosition; } }
			public Quaternion Rotation { get { if (_isCached == false) { _transform.GetPositionAndRotation(out _cachedPosition, out _cachedRotation); _isCached = true; } return _cachedRotation; } }

			public void GetPositionAndRotation(out Vector3 position, out Quaternion rotation)
			{
				if (_isCached == false)
				{
					_transform.GetPositionAndRotation(out _cachedPosition, out _cachedRotation);
					_isCached = true;
				}

				position = _cachedPosition;
				rotation = _cachedRotation;
			}

			public float GetSpeed(float baseSpeed)
			{
				if (SpeedOverride > 0.0f)
				{
					baseSpeed = SpeedOverride;
				}
				else if (SpeedMultiplier > 0.0f)
				{
					baseSpeed *= SpeedMultiplier;
				}

				return baseSpeed;
			}
		}

		private enum EPlatformMode
		{
			None     = 0,
			Looping  = 1,
			PingPong = 2,
		}
	}
}
