namespace Example.TopDownMovement
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Top-down player implementation. Similar to Advanced Movement example.
	/// Supports only mouse and keyboard controls.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	public sealed class TopDownPlayer : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Vector2 _cameraAngles = new Vector2(25.0f, 87.5f); // Camera min/max pitch.
		[SerializeField]
		private Vector2 _cameraDistances = new Vector2(5.0f, 30.0f); // Camera distance at min/max pitch.

		// Camera look rotation is stored separately in player state.
		// This way we can control the KCC "look" rotation independently.
		[Networked]
		private ref TopDownPlayerState _state => ref MakeRef<TopDownPlayerState>();

		private KCC                _kcc;
		private TopDownPlayerInput _input;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (HasStateAuthority == true)
			{
				// Set initial look rotation of the camera. Rotation of the KCC remains default.
				_state.CameraLookRotation = new Vector2(60.0f, 0.0f);
			}
		}

		public override void FixedUpdateNetwork()
		{
			// Apply input look rotation delta to camera look rotation.
			_state.CameraLookRotation = KCCUtility.GetClampedEulerLookRotation(_state.CameraLookRotation, _input.CurrentInput.LookRotationDelta, _cameraAngles.x, _cameraAngles.y);

			// Calculate Yaw only to rotate input direction.
			Quaternion cameraYRotation = Quaternion.Euler(new Vector3(0.0f, _state.CameraLookRotation.y, 0.0f));

			// Set world space input direction. This value is processed later when KCC executes its FixedUpdateNetwork().
			// By default the value is processed by EnvironmentProcessor - which defines base character speed, handles acceleration/friction, gravity and many other features.
			Vector3 inputDirection = cameraYRotation * new Vector3(_input.CurrentInput.MoveDirection.x, 0.0f, _input.CurrentInput.MoveDirection.y);
			_kcc.SetInputDirection(inputDirection);

			if (inputDirection.IsAlmostZero() == false)
			{
				// the KCC faces move direction. This propagates to Transform component immediately.
				_kcc.SetLookRotation(Quaternion.LookRotation(inputDirection));
			}

			// Comparing current input to previous input - this prevents glitches when input is lost.
			if (_input.CurrentInput.Actions.WasPressed(_input.PreviousInput.Actions, TopDownInput.JUMP_BUTTON) == true)
			{
				if (_kcc.Data.IsGrounded == true)
				{
					// Set world space jump vector. This value is processed later when KCC executes its FixedUpdateNetwork().
					_kcc.Jump(Vector3.up * 6.0f);
				}
			}

			if (_kcc.Data.IsGrounded == true)
			{
				// Sprint is a user feature. It is not implemented by KCC by default.
				// Please check SprintProcessor for more details.
				_kcc.SetSprint(_input.CurrentInput.Actions.IsSet(TopDownInput.SPRINT_BUTTON));
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_kcc   = GetComponent<KCC>();
			_input = GetComponent<TopDownPlayerInput>();
		}

		private void LateUpdate()
		{
			// Only input authority needs to update camera.
			if (HasInputAuthority == false)
				return;

			// Update Main Camera position and rotation.
			// Render() is executed before KCC because of [OrderBefore(typeof(KCC))].
			// So we have to do it from LateUpdate() - which is called after Render().

			if (TryGetSnapshotsBuffers(out NetworkBehaviourBuffer from, out NetworkBehaviourBuffer to, out float interpolationAlpha) == false)
				return;

			TopDownPlayerState fromState = from.ReinterpretState<TopDownPlayerState>();
			TopDownPlayerState toState   = to.ReinterpretState<TopDownPlayerState>();

			Vector2 fromLookRotation = KCCUtility.ClampLookRotationAngles(fromState.CameraLookRotation);
			Vector2 toLookRotation   = KCCUtility.ClampLookRotationAngles(toState.CameraLookRotation);

			Vector2 interpolatedCameraLookRotation;
			interpolatedCameraLookRotation.x = KCCUtility.InterpolateRange(fromLookRotation.x, toLookRotation.x, -180.0f, 180.0f, interpolationAlpha);
			interpolatedCameraLookRotation.y = KCCUtility.InterpolateRange(fromLookRotation.y, toLookRotation.y, -180.0f, 180.0f, interpolationAlpha);
			Quaternion offsetRotation = Quaternion.Euler(interpolatedCameraLookRotation);

			float alpha    = 1.0f - (_cameraAngles.y - interpolatedCameraLookRotation.x) / (_cameraAngles.y - _cameraAngles.x);
			float distance = Mathf.Lerp(_cameraDistances.x, _cameraDistances.y, alpha);

			Vector3    cameraPosition = transform.position + offsetRotation * new Vector3(0.0f, 0.0f, -distance);
			Quaternion cameraRotation = Quaternion.LookRotation(transform.position + Vector3.up - cameraPosition);

			Camera.main.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
		}

		// DATA STRUCTURES

		public struct TopDownPlayerState : INetworkStruct
		{
			public Vector2 CameraLookRotation;
		}
	}
}
