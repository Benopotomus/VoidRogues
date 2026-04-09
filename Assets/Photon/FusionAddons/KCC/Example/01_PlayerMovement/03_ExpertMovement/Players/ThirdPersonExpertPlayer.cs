namespace Example.ExpertMovement
{
	using System;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion;
	using Fusion.Addons.KCC;
	using Example.Dash;
	using Example.Portal;
	using Example.Teleport;

	/// <summary>
	/// Expert player implementation with third person view.
	/// </summary>
	public class ThirdPersonExpertPlayer : ExpertPlayer, IPlatformListener, ITeleportListener, IPortalListener
	{
		// PRIVATE MEMBERS

		[SerializeField][Tooltip("KCC should always face move direction.")]
		private bool _faceMoveDirection;
		[SerializeField][Tooltip("Events which trigger look rotation update of KCC.")]
		private ELookRotationUpdateSource _lookRotationUpdateSource = ELookRotationUpdateSource.Jump | ELookRotationUpdateSource.Movement | ELookRotationUpdateSource.MouseHold;
		[SerializeField]
		private LayerMask _cameraCollisionLayerMask;
		[SerializeField]
		private Renderer[] _renderers;

		[Networked]
		private Vector2 _fixedLookRotation { get; set; }

		private Vector2 _baseLookRotation;
		private Vector2 _renderLookRotation;
		private Vector2 _lastFixedLookRotation;
		private float   _cameraDampVelocity;
		private float   _targetCameraDistance;
		private float   _currentCameraDistance;

		// ExpertPlayer INTERFACE

		protected override void OnSpawned()
		{
			// Initialize default values based on object state.
			_cameraDampVelocity    = default;
			_targetCameraDistance  = Vector3.Distance(CameraPivot.position, CameraHandle.position);
			_currentCameraDistance = _targetCameraDistance;

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
			UpdateCamera(true);

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

			// Refreshing camera transforms + propagation to main camera position and rotation.
			UpdateCamera(false);

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

			// Store last look rotation, this is used for render interpolation.
			_lastFixedLookRotation = _fixedLookRotation;

			// Clamp input look rotation delta. Instead of applying immediately to KCC, we store it locally and defer application to the KCC to a point where conditions for application are met.
			// This allows us to rotate with camera around character standing still.
			_fixedLookRotation = KCCUtility.GetClampedEulerLookRotation(_fixedLookRotation, Input.FixedInput.LookRotationDelta, -MaxCameraAngle, MaxCameraAngle);

			bool updateKCCLookRotation = default;

			// Checking look rotation update conditions
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Jump)          == true) { updateKCCLookRotation |= Input.WasActivated(EExpertInputAction.Jump);          }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Movement)      == true) { updateKCCLookRotation |= Input.FixedInput.MoveDirection.IsZero() == false;     }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold)     == true) { updateKCCLookRotation |= Input.FixedInput.RMB;                                 }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseMovement) == true) { updateKCCLookRotation |= Input.FixedInput.LookRotationDelta.IsZero() == false; }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Dash)          == true) { updateKCCLookRotation |= Input.WasActivated(EExpertInputAction.Dash);          }

			Vector3 inputDirection = default;
			Vector2 facingRotation = _fixedLookRotation;

			DashProcessor dashProcessor = KCC.GetModifier<DashProcessor>();
			if (dashProcessor != null)
			{
				// Dash processor detected, we want the visual to face the dash direction.
				// Also force disable facing in look direction on mouse hold.

				facingRotation = new Vector2(facingRotation.x, KCCUtility.GetClampedEulerLookRotation(dashProcessor.Direction.OnlyXZ().normalized).y);
				updateKCCLookRotation = true;
			}
			else
			{
				Vector3 moveDirection = Input.FixedInput.MoveDirection.X0Y();
				if (moveDirection.IsZero() == false)
				{
					// Calculating world space input direction for KCC, update facing and jump rotation based on configuration.
					inputDirection = Quaternion.Euler(0.0f, _fixedLookRotation.y, 0.0f) * moveDirection;

					bool faceCameraDirectionOnMouseHold = HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold) == true && Input.FixedInput.RMB == true;
					if (faceCameraDirectionOnMouseHold == false && _faceMoveDirection == true)
					{
						facingRotation = new Vector2(facingRotation.x, KCCUtility.GetClampedEulerLookRotation(inputDirection.OnlyXZ().normalized).y);
					}
				}
			}

			if (updateKCCLookRotation == true)
			{
				// Some conditions are met, we can apply look rotation to the KCC.
				KCC.SetLookRotation(facingRotation);
			}

			Debug.Log("INPUT " + inputDirection);

			KCC.SetInputDirection(inputDirection);

			if (Input.WasActivated(EExpertInputAction.Jump) == true && KCC.FixedData.IsGrounded == true)
			{
				Quaternion jumpRotation;

				// Is jump rotation invalid (not set)? Get it from other source.
				if (inputDirection.IsZero() == false)
				{
					jumpRotation = Quaternion.LookRotation(inputDirection.OnlyXZ());
				}
				else
				{
					jumpRotation = Quaternion.Euler(0.0f, facingRotation.y, 0.0f);
				}

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

			if (KCC.IsPredictingLookRotation == true)
			{
				// For correct look rotation, we have to apply deltas from all render frames since last fixed update => stored in Input.AccumulatedInput.
				// Additionally we have to apply pending look rotation delta maintained in fixed update, resulting in pending look rotation delta dedicated to render update.
				_renderLookRotation = KCCUtility.GetClampedEulerLookRotation(_baseLookRotation, Input.AccumulatedInput.LookRotationDelta, -MaxCameraAngle, MaxCameraAngle);
			}
			else
			{
				// KCC interpolates look rotation, we'll respect that and interpolate camera as well.
				float alpha = Runner.LocalAlpha;
				_renderLookRotation.x = Mathf.Lerp(_lastFixedLookRotation.x, _fixedLookRotation.x, alpha);
				_renderLookRotation.y = KCCUtility.InterpolateRange(_lastFixedLookRotation.y, _fixedLookRotation.y, -180.0f, 180.0f, alpha);
			}

			bool updateKCCLookRotation = default;

			// Checking look rotation update conditions. These check are done agains Input.AccumulatedInput, because any render input accumulated since last fixed update will trigger look rotation update in next fixed udpate.
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Jump)          == true) { updateKCCLookRotation |= Input.AccumulatedInput.Jump                       == true;  }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.Movement)      == true) { updateKCCLookRotation |= Input.AccumulatedInput.MoveDirection.IsZero()     == false; }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold)     == true) { updateKCCLookRotation |= Input.AccumulatedInput.RMB                        == true;  }
			if (HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseMovement) == true) { updateKCCLookRotation |= Input.AccumulatedInput.LookRotationDelta.IsZero() == false; }

			Vector3 inputDirection  = default;
			Vector3 facingDirection = default;
			Vector2 facingRotation  = _renderLookRotation;

			DashProcessor dashProcessor = KCC.GetModifier<DashProcessor>();
			if (dashProcessor != null)
			{
				// Dash processor detected, we want the visual to face the dash direction.
				// Also force disable facing in look direction on mouse hold.

				facingRotation = new Vector2(facingRotation.x, KCCUtility.GetClampedEulerLookRotation(dashProcessor.Direction.OnlyXZ().normalized).y);
				updateKCCLookRotation = true;
			}
			else
			{
				Vector3 moveDirection = Input.RenderInput.MoveDirection.X0Y();
				if (moveDirection.IsZero() == false)
				{
					// Calculating world space input direction for KCC. Movement strictly depends only on RenderInput.
					inputDirection = Quaternion.Euler(0.0f, _renderLookRotation.y, 0.0f) * moveDirection;

					// Facing and jump direction is the same as input direction.
					facingDirection = inputDirection;
				}
				else
				{
					// Facing and jump direction can be driven by AccumulatedInput as well as a backup.
					moveDirection = Input.AccumulatedInput.MoveDirection.X0Y();
					if (moveDirection.IsZero() == false)
					{
						facingDirection = Quaternion.Euler(0.0f, _renderLookRotation.y, 0.0f) * moveDirection;
					}
				}

				if (facingDirection.IsZero() == false)
				{
					bool faceCameraDirectionOnMouseHold = HasLookRotationUpdateSource(ELookRotationUpdateSource.MouseHold) == true && Input.AccumulatedInput.RMB == true;
					if (faceCameraDirectionOnMouseHold == false && _faceMoveDirection == true)
					{
						facingRotation = new Vector2(facingRotation.x, KCCUtility.GetClampedEulerLookRotation(facingDirection.OnlyXZ().normalized).y);
					}
				}
			}

			if (updateKCCLookRotation == true)
			{
				// Some conditions are met, we can apply look rotation to the KCC.
				KCC.SetLookRotation(facingRotation);
			}

			KCC.SetInputDirection(inputDirection);

			// Jump is predicted for render as well.
			// Checking Input.AccumulatedInput here. Jump accumulated from render inputs since last fixed update will trigger similar code next fixed update.
			// We have to keep the visual to face the direction if there is a jump pending execution in fixed update.
			// We have to check if the KCC was grounded in last fixed update => checking same condition that will be checked next fixed udpate.
			if (Input.AccumulatedInput.Jump == true && KCC.FixedData.IsGrounded == true)
			{
				Quaternion jumpRotation;

				if (facingDirection.IsZero() == false)
				{
					// Facing direction is valid based on input.
					jumpRotation = Quaternion.LookRotation(facingDirection.OnlyXZ());
				}
				else
				{
					// Facing rotation backup from other sources.
					jumpRotation = Quaternion.Euler(0.0f, facingRotation.y, 0.0f);
				}

				if (Input.WasActivated(EExpertInputAction.Jump) == true)
				{
					KCC.Jump(jumpRotation * JumpImpulse);
				}
			}

			// We have to check if the KCC was grounded in last fixed update => checking same condition that will be checked next fixed udpate.
			// Checking RenderData would provide extra responsiveness at the risk of incorrect render prediction decision.
			if (KCC.FixedData.IsGrounded == true)
			{
				// Sprint is updated only when grounded
				KCC.SetSprint(Input.AccumulatedInput.Sprint);
			}
		}

		protected virtual void ProcessInputAfterRenderMovement()
		{
			// Here you can process any other actions after render movement.
			// This gives you extra responsivity at the cost of maintaining render-exclusive prediction.
			// Late processing of render input (for example render predicted shooting) is out of scope of this example.

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

		// IPlatformListener INTERFACE

		void IPlatformListener.OnTransform(KCC kcc, KCCData data, Vector3 positionDelta, Quaternion rotationDelta)
		{
			Vector2 lookRotationDelta = new Vector2(0.0f, rotationDelta.eulerAngles.y);

			_baseLookRotation   = KCCUtility.GetClampedEulerLookRotation(_baseLookRotation,   lookRotationDelta, -MaxCameraAngle, MaxCameraAngle);
			_renderLookRotation = KCCUtility.GetClampedEulerLookRotation(_renderLookRotation, lookRotationDelta, -MaxCameraAngle, MaxCameraAngle);

			if (kcc.IsInFixedUpdate == true)
			{
				_lastFixedLookRotation = _fixedLookRotation;
				_fixedLookRotation     = KCCUtility.GetClampedEulerLookRotation(_fixedLookRotation, lookRotationDelta, -MaxCameraAngle, MaxCameraAngle);
			}

			// Propagate to camera pivot
			CameraPivot.rotation *= rotationDelta;

			RefreshCamera();
		}

		// ITeleportListener INTERFACE

		void ITeleportListener.OnTeleport(KCC kcc, KCCData data)
		{
			HandleTeleport(kcc, data);
		}

		// IPortalListener INTERFACE

		void IPortalListener.OnTeleport(KCC kcc, KCCData data)
		{
			HandleTeleport(kcc, data);
		}

		// PROTECTED METHODS

		protected void UpdateCamera(bool isFixedUpdate)
		{
			if (isFixedUpdate == true)
			{
				// Setting camera pivot rotation.
				CameraPivot.rotation = Quaternion.Euler(_fixedLookRotation);

				_baseLookRotation = _fixedLookRotation;
			}
			else
			{
				// Setting camera pivot location. Because we set the rotation in world-space, it is important to do it after KCC update.
				CameraPivot.rotation = Quaternion.Euler(_renderLookRotation);

				// Update target camera distance only if the cursor is locked.
				if (Cursor.lockState == CursorLockMode.Locked)
				{
					Mouse mouse = Mouse.current;
					if (mouse != null)
					{
						// Target camera distance is modified only by mouse scroll.
						_targetCameraDistance = Mathf.Clamp(_targetCameraDistance - mouse.scroll.ReadValue().y * 0.01f, 2.0f, 8.0f);
					}
				}

				// Smoothly damp to target camera distance after scrolling with mouse or after clamping due to collision with geometry.
				_currentCameraDistance = Mathf.SmoothDamp(_currentCameraDistance, _targetCameraDistance, ref _cameraDampVelocity, 0.15f, 25.0f);
			}

			RefreshCamera();
		}

		protected void RefreshCamera()
		{
			SceneCamera camera = Camera;
			if (camera == null)
				return;

			// Default is CameraHandle transform.
			CameraHandle.GetPositionAndRotation(out Vector3 cameraPosition, out Quaternion cameraRotation);

			// Checking collision with geometry so the Camera transform is not pushed inside.
			Vector3 raycastPosition  = CameraPivot.position;
			Vector3 raycastDirection = cameraPosition - raycastPosition;
			float   raycastDistance  = raycastDirection.magnitude;

			if (raycastDistance > 0.001f)
			{
				raycastDirection /= raycastDistance;
				raycastDistance = _targetCameraDistance;

				PhysicsScene physicsScene = Runner.GetPhysicsScene();
				if (physicsScene.SphereCast(raycastPosition, 0.1f, raycastDirection, out RaycastHit hitInfo, raycastDistance, _cameraCollisionLayerMask, QueryTriggerInteraction.Ignore) == true)
				{
					float hitCameraDistance = Mathf.Max(0.0f, hitInfo.distance - 0.25f);
					if (hitCameraDistance < _currentCameraDistance)
					{
						_currentCameraDistance = hitCameraDistance;
					}
				}

				cameraPosition = raycastPosition + raycastDirection * _currentCameraDistance;
			}

			camera.SetPositionAndRotation(cameraPosition, cameraRotation);

			// Enable/disable renderers based on camera distance from pivot.
			bool showRenderers = _currentCameraDistance > 1.0f;
			for (int i = 0; i < _renderers.Length; ++i)
			{
				_renderers[i].enabled = showRenderers;
			}
		}

		// PRIVATE METHODS

		private void HandleTeleport(KCC kcc, KCCData data)
		{
			// Get look rotation from KCC after teleport and set it as base (this is mandatory if teleport is render predicted).
			_baseLookRotation = data.GetLookRotation(true, true);

			// Set camera look rotation for render updates.
			_renderLookRotation = _baseLookRotation;

			if (kcc.IsInFixedUpdate == true)
			{
				// Set camera look rotation for fixed updates.
				_lastFixedLookRotation = _baseLookRotation;
				_fixedLookRotation     = _baseLookRotation;
			}

			// Propagate to camera pivot
			CameraPivot.rotation = Quaternion.Euler(_baseLookRotation);

			RefreshCamera();
		}

		private void OnKCCSpawn(KCC kcc)
		{
			// Get initial look rotation from the KCC.
			_fixedLookRotation     = kcc.GetLookRotation(true, true);
			_baseLookRotation      = _fixedLookRotation;
			_renderLookRotation    = _fixedLookRotation;
			_lastFixedLookRotation = _fixedLookRotation;

			// We want to update KCC manually to preserve correct execution order.
			kcc.SetManualUpdate(true);
		}

		private bool HasLookRotationUpdateSource(ELookRotationUpdateSource source)
		{
			return (_lookRotationUpdateSource & source) == source;
		}

		// DATA STRUCTURES

		[Flags]
		private enum ELookRotationUpdateSource
		{
			Jump          = 1 << 0, // Look rotation is updated on jump.
			Movement      = 1 << 1, // Look rotation is updated on character movement.
			MouseHold     = 1 << 2, // Look rotation is updated while holding right mouse button.
			MouseMovement = 1 << 3, // Look rotation is updated on mouse move.
			Dash          = 1 << 4, // Look rotation is updated on dash.
		}
	}
}
