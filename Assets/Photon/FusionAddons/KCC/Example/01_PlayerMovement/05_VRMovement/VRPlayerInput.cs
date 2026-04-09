namespace Example.VRMovement
{
	using UnityEngine;
	using UnityEngine.XR;
	using Fusion;

	using XRInputDevice  = UnityEngine.XR.InputDevice;
	using XRCommonUsages = UnityEngine.XR.CommonUsages;

	/// <summary>
	/// Tracks player input.
	/// </summary>
	[DefaultExecutionOrder(-10)]
	public sealed class VRPlayerInput : NetworkBehaviour, IBeforeUpdate, IBeforeTick
	{
		// PUBLIC MEMBERS

		public VRInput CurrentInput     => _currentInput;
		public VRInput PreviousInput    => _previousInput;
		public VRInput AccumulatedInput => _accumulatedInput;

		// PRIVATE MEMBERS

		[SerializeField][Tooltip("Joystick multiplier.")]
		private Vector2 _lookSensitivity = Vector2.one;

		// We need to store current input to compare against previous input (to track actions activation/deactivation). It is also used if the input for current tick is not available.
		// This is not needed on proxies and will be replicated to input authority only.
		[Networked]
		private VRInput _currentInput { get; set; }

		private VRInput _previousInput;
		private VRInput _accumulatedInput;
		private bool    _resetAccumulatedInput;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			// Reset to default state.
			_currentInput          = default;
			_previousInput         = default;
			_accumulatedInput      = default;
			_resetAccumulatedInput = default;

			if (HasInputAuthority == true)
			{
				// Register local player input polling.
				NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();
				networkEvents.OnInput.AddListener(OnInput);

				if (Application.isMobilePlatform == false || Application.isEditor == true)
				{
					// Hide cursor
					Cursor.lockState = CursorLockMode.Locked;
					Cursor.visible   = false;
				}
			}

			// Only local player needs networked properties (current input).
			// This saves network traffic by not synchronizing networked properties to other clients except local player.
			ReplicateToAll(false);
			ReplicateTo(Object.InputAuthority, true);
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			if (runner == null)
				return;

			NetworkEvents networkEvents = runner.GetComponent<NetworkEvents>();
			if (networkEvents != null)
			{
				// Unregister local player input polling.
				networkEvents.OnInput.RemoveListener(OnInput);
			}
		}

		// IBeforeUpdate INTERFACE

		/// <summary>
		/// 1. Collect input from devices, can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed.
		/// </summary>
		void IBeforeUpdate.BeforeUpdate()
		{
			if (HasInputAuthority == false)
				return;

			// Accumulated input was polled and explicit reset requested.
			if (_resetAccumulatedInput == true)
			{
				_resetAccumulatedInput = false;
				_accumulatedInput = default;
			}

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				// Input is tracked only if the cursor is locked.
				if (Cursor.lockState != CursorLockMode.Locked)
					return;
			}

			ProcessVRInput();
		}

		/// <summary>
		/// 3. Read input from Fusion.
		/// </summary>
		void IBeforeTick.BeforeTick()
		{
			if (Object == null)
				return;

			// Set current in input as previous.
			_previousInput = _currentInput;

			// Clear all properties which should not propagate from last known input in case of missing new input. As example, following line will reset look rotation delta.
			// This results to the player not being incorrectly rotated (by using rotation from last known input) in case of missing input on state authority, followed by a correction on the input authority.
			VRInput currentInput = _currentInput;
			currentInput.LookRotationDelta = default;
			_currentInput = currentInput;

			if (Object.InputAuthority != PlayerRef.None)
			{
				// If this fails, the current input won't be updated and input from previous tick will be reused.
				if (Runner.TryGetInputForPlayer(Object.InputAuthority, out VRInput input) == true)
				{
					// New input received, we can store it as current.
					_currentInput = input;
				}
			}
		}

		// PRIVATE METHODS

		/// <summary>
		/// 2. Push accumulated input and reset properties, can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation.
		/// This is usually executed multiple times if there is a performance spike, for example after expensive spawn which includes asset loading.
		/// </summary>
		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			// Set accumulated input.
			networkInput.Set(_accumulatedInput);

			// Now we reset all properties which should not propagate into next OnInput() call (for example LookRotationDelta - this must be applied only once and reset immediately).
			// If there's a spike, OnInput() and FixedUpdateNetwork() will be called multiple times in a row without BeforeUpdate() in between, so we don't reset move direction to preserve movement.
			// Move direction and other properties are reset in next BeforeUpdate() - driven by _resetAccumulatedInput flag.
			_accumulatedInput.LookRotationDelta = default;

			// Input is polled for single fixed update, but at this time we don't know how many times in a row OnInput() will be executed.
			// This is the reason to have a reset flag instead of resetting input immediately, otherwise we could lose input for next fixed updates (for example move direction).
			_resetAccumulatedInput = true;
		}

		private void ProcessVRInput()
		{
			Vector2 moveDirection     = Vector2.zero;
			Vector2 lookRotationDelta = Vector2.zero;

			XRInputDevice head = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
			if (head.isValid == true)
			{
				if (head.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3    headPosition) == true) { _accumulatedInput.HeadPosition = headPosition; }
				if (head.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion headRotation) == true) { _accumulatedInput.HeadRotation = headRotation; }
			}

			XRInputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			if (leftHand.isValid == true)
			{
				if (leftHand.TryGetFeatureValue(XRCommonUsages.triggerButton,  out bool       leftTriggerButtonUsed) == true) { _accumulatedInput.Actions.Set(VRInput.LT_BUTTON, leftTriggerButtonUsed); }
				if (leftHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3    leftHandPosition)      == true) { _accumulatedInput.LeftHandPosition = leftHandPosition; }
				if (leftHand.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion leftHandRotation)      == true) { _accumulatedInput.LeftHandRotation = leftHandRotation; }

				if (leftHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 movement) == true)
				{
					moveDirection = movement;
				}
			}

			XRInputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
			if (rightHand.isValid == true)
			{
				if (rightHand.TryGetFeatureValue(XRCommonUsages.triggerButton,  out bool       rightTriggerButtonUsed) == true) { _accumulatedInput.Actions.Set(VRInput.RT_BUTTON, rightTriggerButtonUsed); }
				if (rightHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3    rightHandPosition)      == true) { _accumulatedInput.RightHandPosition = rightHandPosition; }
				if (rightHand.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rightHandRotation)      == true) { _accumulatedInput.RightHandRotation = rightHandRotation; }

				if (rightHand.TryGetFeatureValue(XRCommonUsages.primary2DAxis, out Vector2 look) == true)
				{
					lookRotationDelta.y = look.x;
				}
			}

			_accumulatedInput.MoveDirection = moveDirection;
			_accumulatedInput.LookRotationDelta += lookRotationDelta * _lookSensitivity;
		}
	}
}
