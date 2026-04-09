namespace Example.VRMovement
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Special VR player implementation which uses 2 KCC instances.
	/// Root KCC - moves your root (headset origin) in world space. This object.
	/// Visual KCC - moves in your headset local space. Separate networked object.
	/// </summary>
	[DefaultExecutionOrder(-5)]
	[RequireComponent(typeof(KCC))]
	public sealed class VRPlayer : NetworkBehaviour
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private KCC _visualPrefab;

		[Networked]
		private ref VRPlayerState _state => ref MakeRef<VRPlayerState>();
		[Networked]
		private KCC _visualKCC { get; set; }

		private VRPlayerInput  _input;
		private VRPlayerVisual _visual;
		private SceneCamera    _camera;
		private KCC            _rootKCC;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			name = Object.InputAuthority.ToString();

			if (HasStateAuthority == true)
			{
				// Visual KCC is spawned on state authority only and synced later in RefreshVisual().
				_visualKCC = Runner.Spawn(_visualPrefab, transform.position, transform.rotation, Object.InputAuthority);
				_visualKCC.Object.SetPlayerAlwaysInterested(Object.InputAuthority, true);
			}

			if (HasInputAuthority == true)
			{
				_camera = Runner.SimulationUnityScene.FindComponent<SceneCamera>(false);
			}

			RefreshVisual();

			// We don't know if the KCC is already spawned at this point.
			// KCC.InvokeOnSpawn() ensures the callback is executed after KCC.Spawned() and its API called in proper order.
			_rootKCC.InvokeOnSpawn(InitializeKCC);
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (_visualKCC != null && _visualKCC.Object != null)
			{
				// Visual KCC is owned by this object.
				Runner.Despawn(_visualKCC.Object);
			}

			_camera    = null;
			_visual    = null;
			_visualKCC = null;
		}

		public override sealed void FixedUpdateNetwork()
		{
			RefreshVisual();

			if (_visual == null)
				return;

			// For following lines, we should use Input.FixedInput only. This property holds input for fixed updates.

			// Clamp input look rotation delta
			Vector2 lookRotation      = _rootKCC.FixedData.GetLookRotation(true, true);
			Vector2 lookRotationDelta = KCCUtility.GetClampedEulerLookRotationDelta(lookRotation, _input.CurrentInput.LookRotationDelta, -90.0f, 90.0f);

			// Apply clamped look rotation delta
			_rootKCC.AddLookRotation(lookRotationDelta);

			// Calculate input direction based on recently updated look rotation (the change propagates internally also to KCCData.TransformRotation)
			Vector3 inputDirection = _rootKCC.FixedData.TransformRotation * new Vector3(_input.CurrentInput.MoveDirection.x, 0.0f, _input.CurrentInput.MoveDirection.y);
			_rootKCC.SetInputDirection(inputDirection);

			// Update the Root KCC
			_rootKCC.ManualFixedUpdate();

			// Calculate world space position and rotation offsets between Root KCC (world space) and head (local space)
			Vector3 positionOffset = _rootKCC.FixedData.TransformRotation * _input.CurrentInput.HeadPosition.OnlyXZ();
			Vector3 rotationOffset = _input.CurrentInput.HeadRotation.eulerAngles;

			// Apply calculated values on top of values from Root KCC
			_visualKCC.SetPosition(_rootKCC.FixedData.TargetPosition + positionOffset);
			_visualKCC.SetLookRotation(_rootKCC.FixedData.LookPitch + rotationOffset.x, _rootKCC.FixedData.LookYaw + rotationOffset.y);

			// Update the Visual KCC
			_visualKCC.ManualFixedUpdate();

			// Store hands position for network synchronization
			_state.LeftHandPosition  = _input.CurrentInput.LeftHandPosition;
			_state.LeftHandRotation  = _input.CurrentInput.LeftHandRotation;
			_state.RightHandPosition = _input.CurrentInput.RightHandPosition;
			_state.RightHandRotation = _input.CurrentInput.RightHandRotation;

			// Update visual
			_visual.LeftHand.localPosition  = _state.LeftHandPosition;
			_visual.LeftHand.localRotation  = _state.LeftHandRotation;
			_visual.RightHand.localPosition = _state.RightHandPosition;
			_visual.RightHand.localRotation = _state.RightHandRotation;

			// Comparing current input to previous input - this prevents glitches when input is lost.
			if (_input.CurrentInput.Actions.WasPressed(_input.PreviousInput.Actions, VRInput.LT_BUTTON) == true)
			{
				// Left trigger button action
			}

			if (_input.CurrentInput.Actions.WasPressed(_input.PreviousInput.Actions, VRInput.RT_BUTTON) == true)
			{
				// Right trigger button action
			}

			// Additional input processing goes here
		}

		public override sealed void Render()
		{
			RefreshVisual();

			if (_visual == null)
				return;

			if (HasInputAuthority == true)
			{
				// For following lines, we should use Input.RenderInput and Input.AccumulatedInput only. These properties hold input for render updates.
				// Input.RenderInput holds input for current render frame.
				// Input.AccumulatedInput holds combined input for all render frames from last fixed update. This property will be used to set input for next fixed update (polled by Fusion).

				// Look rotation have to be updated to get smooth camera rotation

				// Get look rotation from last fixed update (not last render!)
				Vector2 lookRotation = _rootKCC.FixedData.GetLookRotation(true, true);

				// For correct look rotation, we have to apply deltas from all render frames since last fixed update => stored in Input.AccumulatedInput
				Vector2 lookRotationDelta = KCCUtility.GetClampedEulerLookRotationDelta(lookRotation, _input.AccumulatedInput.LookRotationDelta, -90.0f, 90.0f);

				_rootKCC.SetLookRotation(lookRotation + lookRotationDelta);

				// Update the Root KCC
				_rootKCC.ManualRenderUpdate();

				// Calculate world space position and rotation offsets between Root KCC (world space) and head (local space)
				Vector3 positionOffset = _rootKCC.RenderData.TransformRotation * _input.AccumulatedInput.HeadPosition.OnlyXZ();
				Vector3 rotationOffset = _input.AccumulatedInput.HeadRotation.eulerAngles;

				// Apply calculated values on top of values from Root KCC
				_visualKCC.SetPosition(_rootKCC.RenderData.TargetPosition + positionOffset);
				_visualKCC.SetLookRotation(_rootKCC.RenderData.LookPitch + rotationOffset.x, _rootKCC.RenderData.LookYaw + rotationOffset.y);

				// Update the Visual KCC
				_visualKCC.ManualRenderUpdate();
			}
			else
			{
				// Update both KCCs
				_rootKCC.ManualRenderUpdate();
				_visualKCC.ManualRenderUpdate();

				if (TryGetSnapshotsBuffers(out NetworkBehaviourBuffer from, out NetworkBehaviourBuffer to, out float interpolationAlpha) == true)
				{
					VRPlayerState fromState = from.ReinterpretState<VRPlayerState>();
					VRPlayerState toState   = to.ReinterpretState<VRPlayerState>();

					_visual.LeftHand.localPosition  = Vector3.Lerp(fromState.LeftHandPosition,  toState.LeftHandPosition,  interpolationAlpha);
					_visual.RightHand.localPosition = Vector3.Lerp(fromState.RightHandPosition, toState.RightHandPosition, interpolationAlpha);

					if (HasValidRotations(fromState.LeftHandRotation, toState.LeftHandRotation) == true)
					{
						_visual.LeftHand.localRotation = Quaternion.Lerp(fromState.LeftHandRotation, toState.LeftHandRotation, interpolationAlpha);
					}

					if (HasValidRotations(fromState.RightHandRotation, toState.RightHandRotation) == true)
					{
						_visual.RightHand.localRotation = Quaternion.Lerp(fromState.RightHandRotation, toState.RightHandRotation, interpolationAlpha);
					}
				}
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_input   = gameObject.GetComponent<VRPlayerInput>();
			_rootKCC = gameObject.GetComponent<KCC>();
		}

		private void LateUpdate()
		{
			if (HasInputAuthority == false)
				return;

			// Refresh VR pose and camera for local player.

			VRPose vrPose = VRPose.Get();

			if (_visual != null)
			{
				_visual.LeftHand.position  = _rootKCC.RenderData.TargetPosition + _rootKCC.RenderData.TransformRotation * vrPose.LeftHandPosition;
				_visual.LeftHand.rotation  = _rootKCC.RenderData.TransformRotation * vrPose.LeftHandRotation;
				_visual.RightHand.position = _rootKCC.RenderData.TargetPosition + _rootKCC.RenderData.TransformRotation * vrPose.RightHandPosition;
				_visual.RightHand.rotation = _rootKCC.RenderData.TransformRotation * vrPose.RightHandRotation;
			}

			if (_camera != null)
			{
				Vector3    cameraPosition = _rootKCC.RenderData.TargetPosition + _rootKCC.RenderData.TransformRotation * vrPose.HeadPosition;
				Quaternion cameraRotation = _rootKCC.RenderData.TransformRotation * vrPose.HeadRotation;

				_camera.SetPositionAndRotation(cameraPosition, cameraRotation);
			}
		}

		// PRIVATE METHODS

		private void InitializeKCC(KCC kcc)
		{
			if (kcc.IsSpawned == false)
			{
				// Refactoring safety check. This method is called from incorrect code path.
				throw new System.InvalidOperationException("The KCC is not spawned yet!");
			}

			// The KCC.Spawned() has been already called and we can safely use its API.

			// We want to update KCC manually to preserve correct execution order.
			kcc.SetManualUpdate(true);
		}

		private void RefreshVisual()
		{
			if (_visualKCC == null || _visualKCC.Object == null)
			{
				_visual = null;
				return;
			}

			if (_visualKCC.HasManualUpdate == false)
			{
				// Both KCCs are updated manually in specific order.
				_visualKCC.SetManualUpdate(true);
			}

			if (_visual == null)
			{
				_visual = _visualKCC.GetComponent<VRPlayerVisual>();
				_visual.name = $"{name} Visual";

				if (HasInputAuthority == true)
				{
					// Only hands are visible to local player.
					_visual.SetVisibility(false, false, true);
				}
			}
		}

		private static bool HasValidRotations(Quaternion rotation1, Quaternion rotation2)
		{
			return rotation1.IsNaN() == false && rotation1.IsZero() == false && rotation2.IsNaN() == false && rotation2.IsZero() == false;
		}

		// DATA STRUCTURES

		public struct VRPlayerState : INetworkStruct
		{
			// Hands are networked so other players can see them. Head position/rotation is propagated to the Visual KCC and doesn't need to be synced.
			public Vector3    LeftHandPosition;
			public Quaternion LeftHandRotation;
			public Vector3    RightHandPosition;
			public Quaternion RightHandRotation;
		}
	}
}
