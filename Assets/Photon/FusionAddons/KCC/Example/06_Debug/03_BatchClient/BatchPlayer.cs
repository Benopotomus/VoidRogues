namespace Example.BatchClient
{
	using System.Collections;
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Batch player implementation used to generate input.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public sealed class BatchPlayer : NetworkBehaviour
	{
		// PUBLIC MEMBERS

		public KCC KCC;

		public bool IsServerControlled => _isServerControlled;

		// PRIVATE MEMBERS

		[Networked]
		private Vector3 TargetWaypointPosition { get; set; }

		private BatchInput _input;
		private Vector2[]  _areasOfInterest;
		private Vector3    _lastCheckInfo;
		private float      _pendingLookRotation;
		private bool       _isServerControlled;

		private Vector2[][] _aoiPresets = new Vector2[][]
		{
			new Vector2[] { new Vector2(25.0f, 50.0f), new Vector2(100.0f, 75.0f) },
			new Vector2[] { new Vector2(25.0f, 50.0f), new Vector2(100.0f, 75.0f), new Vector2(175.0f, 100.0f) },
			new Vector2[] { new Vector2(50.0f, 100.0f), new Vector2(175.0f, 150.0f) },
			new Vector2[] { new Vector2(0.0f, 50.0f) },
			new Vector2[] { new Vector2(0.0f, 100.0f) },
			new Vector2[] { new Vector2(0.0f, 150.0f) },
		};

		// PUBLIC METHODS

		public void SetServerControlled()
		{
			if (_isServerControlled == true || Runner.IsServer == false)
				return;

			_isServerControlled = true;

			StartCoroutine(GenerateLookRotatationCoroutine());
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (HasStateAuthority == true)
			{
				FindTargetWaypoint();
			}

			if (HasInputAuthority == true)
			{
				// Register input polling for local player.
				Runner.GetComponent<NetworkEvents>().OnInput.AddListener(OnPlayerInput);

				StartCoroutine(GenerateLookRotatationCoroutine());
			}
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			// Unregister input polling.
			runner.GetComponent<NetworkEvents>().OnInput.RemoveListener(OnPlayerInput);

			StopAllCoroutines();

			_isServerControlled = false;
		}

		public override void FixedUpdateNetwork()
		{
			PlayerRef inputAuthority = Object.InputAuthority;

			bool hasInput = Runner.TryGetInputForPlayer(inputAuthority, out BatchInput input);
			if (_isServerControlled == true)
			{
				hasInput = true;
				input = _input;
				_input = default;
			}

			if (hasInput == true)
			{
				// Apply look rotation delta. This propagates to Transform component immediately.
				KCC.AddLookRotation(input.LookRotationDelta);

				// Set world space input direction. This value is processed later when KCC executes its FixedUpdateNetwork().
				KCC.SetInputDirection(input.MoveDirection);

				if (input.Jump == true && KCC.Data.IsGrounded == true)
				{
					// Set world space jump vector. This value is processed later when KCC executes its FixedUpdateNetwork().
					KCC.Jump(Vector3.up * 6.0f);
				}

				if (KCC.FixedData.TargetPosition.y < -10.0f)
				{
					// Player is falling, reset...
					KCC.SetPosition(KCC.FixedData.TargetPosition.OnlyXZ());
					FindTargetWaypoint();
				}
			}

			if (_areasOfInterest.Length > 0 && Runner.IsLastTick == true && HasStateAuthority == true && Runner.IsPlayerValid(inputAuthority) == true)
			{
				Runner.ClearPlayerAreaOfInterest(inputAuthority);

				Vector3 basePosition  = KCC.FixedData.TargetPosition;
				Vector3 baseDirection = KCC.FixedData.LookDirection;

				// Following call sets AoI (Area of Interest) for owner player.
				// X = Distance of the area from player (origin).
				// Y = Radius of the area.
				for (int i = 0, count = _areasOfInterest.Length; i < count; ++i)
				{
					Runner.AddPlayerAreaOfInterest(inputAuthority, basePosition + baseDirection * _areasOfInterest[i].x, _areasOfInterest[i].y);
				}
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			ApplicationUtility.GetCommandLineArgument("-aoiPreset", out int aoiPreset);

			_areasOfInterest = _aoiPresets[aoiPreset];
		}

		private void LateUpdate()
		{
			if (KCC.IsSpawned == false)
				return;

			if (KCC.HasStateAuthority == true)
			{
				_lastCheckInfo.y -= Time.unscaledDeltaTime;
				if (_lastCheckInfo.y < 0.0f)
				{
					_lastCheckInfo.y = 3.0f;

					Vector3 checkPositionXZ     = _lastCheckInfo.OnlyXZ();
					Vector3 transformPositionXZ = transform.position.OnlyXZ();

					if (Vector3.Distance(transformPositionXZ, checkPositionXZ) < 0.25f)
					{
						FindTargetWaypoint();
					}

					_lastCheckInfo.x = transformPositionXZ.x;
					_lastCheckInfo.z = transformPositionXZ.z;
				}
			}

			if (KCC.HasInputAuthority == true || _isServerControlled == true)
			{
				_input.MoveDirection = (TargetWaypointPosition - transform.position).OnlyXZ().normalized;

				const float ROTATION_SPEED = 120.0f;

				if (_pendingLookRotation > 5.0f)
				{
					_pendingLookRotation -= Time.deltaTime * ROTATION_SPEED;
					_input.LookRotationDelta.y += Time.deltaTime * ROTATION_SPEED;
				}
				else if (_pendingLookRotation < -5.0f)
				{
					_pendingLookRotation += Time.deltaTime * ROTATION_SPEED;
					_input.LookRotationDelta.y -= Time.deltaTime * ROTATION_SPEED;
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			if (KCC.IsSpawned == false)
				return;

			Vector3 basePosition  = KCC.FixedData.TargetPosition;
			Vector3 baseDirection = KCC.FixedData.LookDirection;

			for (int i = 0, count = _areasOfInterest.Length; i < count; ++i)
			{
				Gizmos.DrawWireSphere(basePosition + baseDirection * _areasOfInterest[i].x, _areasOfInterest[i].y);
			}
		}

		// PRIVATE METHDOS

		private void FindTargetWaypoint()
		{
			Waypoints waypoints = Runner.SimulationUnityScene.FindComponent<Waypoints>();
			if (waypoints == null)
				return;

			TargetWaypointPosition = waypoints.GetRandomWaypoint().position;
		}

		private void OnPlayerInput(NetworkRunner runner, NetworkInput networkInput)
		{
			networkInput.Set(_input);
			_input = default;
		}

		private IEnumerator GenerateLookRotatationCoroutine()
		{
			const float maxAngle   = 180.0f;
			const float upperLimit = 30.0f;
			const float lowerLimit = -30.0f;

			while (true)
			{
				_pendingLookRotation = Random.Range(-maxAngle, maxAngle);
				if (_pendingLookRotation > lowerLimit &&_pendingLookRotation < upperLimit)
					continue;

				yield return new WaitForSecondsRealtime(Random.Range(1.0f, 5.0f));
			}
		}
	}
}
