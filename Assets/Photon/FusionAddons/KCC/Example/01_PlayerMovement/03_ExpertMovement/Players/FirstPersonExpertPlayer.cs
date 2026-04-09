namespace Example.ExpertMovement
{
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion.Addons.KCC;
	using Example.Dash;

	/// <summary>
	/// Expert player implementation with first person view.
	/// This implementation sets look rotation directly to KCC and Camera is then synced.
	/// </summary>
	public class FirstPersonExpertPlayer : ExpertPlayer
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private GameObject _visual;

		private float _targetCameraFOV;
		private float _currentCameraFOV;
		private float _cameraDampVelocity;

		// ExpertPlayer INTERFACE

		protected override void OnSpawned()
		{
			// Initialize default values.
			_targetCameraFOV    = default;
			_currentCameraFOV   = default;
			_cameraDampVelocity = default;

			// Disable visual for local player.
			_visual.SetActive(HasInputAuthority == false);

			// We don't know if the KCC has already been spawned at this point.
			// KCC.InvokeOnSpawn() ensures the callback is executed after KCC.Spawned() and its API used in proper order.
			KCC.InvokeOnSpawn(OnKCCSpawn);
		}

		protected override void OnFixedUpdate()
		{
			// Update movement / look properties first.
			ProcessInputBeforeFixedMovement();

			// All movement related properties set, we can trigger manual KCC update.
			KCC.ManualFixedUpdate();

			// Refreshing camera transforms + propagation to main camera position and rotation.
			RefreshCamera();

			// Process other input actions after movement and camera sync.
			ProcessInputAfterFixedMovement();
		}

		protected override void OnSortedUpdate()
		{
			// Process other input actions sorted by ExpertInput.LocalAlpha.
			ProcessInputSortedByLocalAlpha();
		}

		protected override void OnRenderUpdate()
		{
			bool hasRenderInput = HasInputAuthority;
			if (hasRenderInput == true)
			{
				// Update movement / look first properties first.
				ProcessInputBeforeRenderMovement();
			}

			// All movement related properties set, we can trigger manual KCC update.
			KCC.ManualRenderUpdate();

			// Updating desired camera FOV.
			if (_targetCameraFOV != default)
			{
				// Update target camera FOV only if the cursor is locked.
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Mouse mouse = Mouse.current;
					if (mouse != null)
					{
						// Target camera FOV is modified only by mouse scroll.
						_targetCameraFOV = Mathf.Clamp(_targetCameraFOV - mouse.scroll.ReadValue().y * 0.1f, 20.0f, 90.0f);
					}
				}

				// Smoothly damp to target camera FOV after scrolling with mouse.
				_currentCameraFOV = Mathf.SmoothDamp(_currentCameraFOV, _targetCameraFOV, ref _cameraDampVelocity, 0.05f, 200.0f);
			}

			// Refreshing camera transforms + propagation to main camera position and rotation.
			RefreshCamera();

			if (hasRenderInput == true)
			{
				// Optionally process other input actions.
				// This gives you extra responsivity at the cost of increased complexity.
				// Depending of system setup (mouse polling rate, monitor refresh rate, engine render rate, Fusion simulation rate)
				// the input lag can be decreased to 2-4ms (measured with NVIDIA Reflex Analyzer).
				ProcessInputAfterRenderMovement();
			}
		}

		// PROTECTED METHODS

		protected virtual void ProcessInputBeforeFixedMovement()
		{
			// Here we process input for fixed update and set properties related to movement / look.
			// For following lines, we should use Input.FixedInput only. This property holds input for fixed update.

			// Clamp input look rotation delta.
			Vector2 lookRotation      = KCC.FixedData.GetLookRotation(true, true);
			Vector2 lookRotationDelta = KCCUtility.GetClampedEulerLookRotationDelta(lookRotation, Input.FixedInput.LookRotationDelta, -MaxCameraAngle, MaxCameraAngle);

			// Apply clamped look rotation delta.
			KCC.AddLookRotation(lookRotationDelta);

			// Calculate input direction based on recently updated look rotation (the change propagates internally also to KCCData.TransformRotation).
			Vector3 inputDirection = KCC.FixedData.TransformRotation * new Vector3(Input.FixedInput.MoveDirection.x, 0.0f, Input.FixedInput.MoveDirection.y);

			KCC.SetInputDirection(inputDirection);

			if (Input.WasActivated(EExpertInputAction.Jump) == true && KCC.FixedData.IsGrounded == true)
			{
				// By default the character jumps forward in facing direction.
				Quaternion jumpRotation = KCC.FixedData.TransformRotation;

				if (inputDirection.IsAlmostZero() == false)
				{
					// If we are moving, jump in that direction instead.
					jumpRotation = Quaternion.LookRotation(inputDirection);
				}

				// Applying jump impulse.
				KCC.Jump(jumpRotation * JumpImpulse);
			}

			// Notice we are checking KCC.FixedData because we are in fixed update code path (render update uses KCC.RenderData).
			if (KCC.FixedData.IsGrounded == true)
			{
				// Sprint is updated only when grounded.
				KCC.SetSprint(Input.FixedInput.Sprint);
			}

			if (Input.WasActivated(EExpertInputAction.Dash) == true)
			{
				if (Abilities.TryGetAbility(out DashProcessor dashAbility) == true)
				{
					// Dash is movement related action, should be processed before KCC ticks.
					// We only care about registering processor to the KCC, responsibility for cleanup is on dash processor.
					KCC.AddModifier(dashAbility);
				}
			}

			// Another movement related actions here (crouch, ...)
		}

		protected virtual void ProcessInputAfterFixedMovement()
		{
			// Process other input actions after movement.
			// Player and Camera positions are already updated.

			if (Input.WasActivated(EExpertInputAction.RMB) == true)
			{
				// Right mouse button action.
			}

			if (Input.WasActivated(EExpertInputAction.MMB) == true)
			{
				// Middle mouse button action.
			}
		}

		protected virtual void ProcessInputSortedByLocalAlpha()
		{
			// Process other input actions sorted by ExpertInput.LocalAlpha.

			if (Input.WasActivated(EExpertInputAction.LMB) == true)
			{
				// Left mouse button action.

				// This sample supports NVIDIA Reflex Analyzer to measure system latency.
				// You must enable GameplayUI.prefab => NVIDIAReflex game object to see indicators.
				foreach (GameplayUI gameplayUI in Runner.GetAllBehaviours<GameplayUI>())
				{
					gameplayUI.SetReflexIndicatorActive();
				}
			}
		}

		protected virtual void ProcessInputBeforeRenderMovement()
		{
			// Here we process input for render update and set properties related to movement / look.
			// For following lines, we should use Input.RenderInput and Input.AccumulatedInput only.
			// Input.RenderInput holds input for current render frame.
			// Input.AccumulatedInput holds combined input for all render frames from last fixed update. This input will be used for next fixed update.

			// Get look rotation from last fixed update (not  render).
			Vector2 lookRotation = KCC.FixedData.GetLookRotation(true, true);

			// For correct look rotation, we have to apply deltas from all render frames since last fixed update => stored in Input.AccumulatedInput.
			Vector2 lookRotationDelta = KCCUtility.GetClampedEulerLookRotationDelta(lookRotation, Input.AccumulatedInput.LookRotationDelta, -MaxCameraAngle, MaxCameraAngle);

			KCC.SetLookRotation(lookRotation + lookRotationDelta);

			Vector3 inputDirection = default;

			// MoveDirection values from previous render frames are already consumed and applied by KCC, so we use Input.RenderInput (non-accumulated input for this frame).
			Vector3 moveDirection = Input.RenderInput.MoveDirection.X0Y();
			if (moveDirection.IsZero() == false)
			{
				inputDirection = KCC.RenderData.TransformRotation * moveDirection;
			}

			KCC.SetInputDirection(inputDirection);

			// Jump is predicted for render as well.
			// We have to check if the KCC was grounded in last fixed update => checking same condition that will be checked next fixed udpate.
			if (Input.WasActivated(EExpertInputAction.Jump) == true && KCC.FixedData.IsGrounded == true)
			{
				// By default the character jumps in forward direction.
				Quaternion jumpRotation = KCC.RenderData.TransformRotation;

				if (inputDirection.IsZero() == false)
				{
					// If we are moving, jump in that direction instead.
					jumpRotation = Quaternion.LookRotation(inputDirection);
				}

				KCC.Jump(jumpRotation * JumpImpulse);
			}

			// We have to check if the KCC was grounded in last fixed update => checking same condition that will be checked next fixed udpate.
			// Checking RenderData would provide extra responsiveness at the risk of incorrect render prediction decision.
			if (KCC.FixedData.IsGrounded == true)
			{
				// Sprint is updated only when grounded.
				KCC.SetSprint(Input.AccumulatedInput.Sprint);
			}

			// At his point, KCC haven't been updated yet, we only set input properties (except look rotation, which propagates to Transform immediately) so camera have to be synced later.
		}

		protected virtual void ProcessInputAfterRenderMovement()
		{
			// Here you can process any other actions after render movement.
			// This gives you extra responsivity at the cost of maintaining render-exclusive prediction.
			// Late processing of render input (for example render predicted shooting) is out of scope of this example.
			// Player and Camera positions are already updated.

			if (Input.WasActivated(EExpertInputAction.LMB) == true)
			{
				// Left mouse button action.
				// Note that Input.WasActivated() returns true ONLY if it wasn't already consumed by forward fixed simulation in the same frame.

				// This sample supports NVIDIA Reflex Analyzer to measure system latency.
				// You must enable GameplayUI.prefab => NVIDIAReflex game object to see indicators.
				foreach (GameplayUI gameplayUI in Runner.GetAllBehaviours<GameplayUI>())
				{
					gameplayUI.SetReflexIndicatorActive();
				}
			}
		}

		protected void RefreshCamera()
		{
			// Updating camera pivot based on character rotation.
			Vector2 pitchRotation = KCC.Data.GetLookRotation(true, false);
			CameraPivot.localRotation = Quaternion.Euler(pitchRotation);

			SceneCamera camera = Camera;
			if (camera == null)
				return;

			// Default is CameraHandle transform.
			CameraHandle.GetPositionAndRotation(out Vector3 cameraPosition, out Quaternion cameraRotation);

			camera.SetPositionAndRotation(cameraPosition, cameraRotation);

			if (_targetCameraFOV == default)
			{
				// FOV initialization.
				_targetCameraFOV  = camera.GetActiveCamera().fieldOfView;
				_currentCameraFOV = _targetCameraFOV;
			}

			camera.SetFieldOfView(_currentCameraFOV);
		}

		// PRIVATE METHODS

		private void OnKCCSpawn(KCC kcc)
		{
			// We want to update KCC manually to preserve correct execution order.
			kcc.SetManualUpdate(true);
		}
	}
}
