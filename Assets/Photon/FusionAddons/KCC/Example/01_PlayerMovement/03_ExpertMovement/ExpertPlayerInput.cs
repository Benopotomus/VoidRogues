namespace Example.ExpertMovement
{
	using System;
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Tracks player input for fixed and render updates.
	/// </summary>
	[DefaultExecutionOrder(-10)]
	public sealed partial class ExpertPlayerInput : NetworkBehaviour, IBeforeAllTicks, IBeforeTick, IAfterAllTicks
	{
		// PUBLIC MEMBERS

		/// <summary>
		/// Holds input for fixed update.
		/// </summary>
		public ExpertInput FixedInput { get { CheckFixedAccess(false); return _fixedInput; } }

		/// <summary>
		/// Holds input for current frame render update.
		/// </summary>
		public ExpertInput RenderInput { get { CheckRenderAccess(false); return _renderInput; } }

		/// <summary>
		/// Holds accumulated inputs from all render frames since last fixed update. Used when Fusion input poll is triggered.
		/// </summary>
		public ExpertInput AccumulatedInput { get { CheckRenderAccess(false); return _accumulatedInput; } }

		/// <summary>
		/// Indicates the input is ignored after resuming app to prevent glitches until the simulation is stable.
		/// </summary>
		public bool IsIgnoringInput => _ignoreTime > 0.0f;

		/// <summary>
		/// Set custom look responsivity (mouse input smoothing).
		/// </summary>
		public float LookResponsivity { get { return _lookResponsivity; } set { _lookResponsivity = value; } }

		/// <summary>
		/// These actions won't be accumulated and polled by Fusion if they are triggered in the same frame as the simulation.
		/// They are accumulated after Fusion simulation and before Render(), effectively defering actions to first fixed simulation in following frames.
		/// This makes fixed and render-predicted movement much more consistent (less prediction correction) at the cost of slight delay.
		/// </summary>
		[NonSerialized]
		public EExpertInputAction[] DeferredInputActions = new EExpertInputAction[] { EExpertInputAction.LMB, EExpertInputAction.Jump, EExpertInputAction.Dash };

		/// <summary>
		/// These actions trigger sending interpolation data required for render-accurate lag compensation queries.
		/// Like DeferredInputActions, these actions won't be accumulated and polled by Fusion if they are triggered in the same frame as the simulation.
		/// They are accumulated after Fusion simulation and before Render(), effectively defering actions to first fixed simulation in following frames.
		/// </summary>
		[NonSerialized]
		public EExpertInputAction[] InterpolationDataActions = new EExpertInputAction[] { EExpertInputAction.LMB };

		// PRIVATE MEMBERS

		[SerializeField][Tooltip("Mouse delta multiplier.")]
		private Vector2 _standaloneLookSensitivity = Vector2.one;
		[SerializeField][Tooltip("Touch delta multiplier.")]
		private Vector2 _mobileLookSensitivity = Vector2.one;
		[SerializeField][Tooltip("Gamepad stick multiplier.")]
		private Vector2 _gamepadLookSensitivity = Vector2.one;
		[SerializeField][Range(0.0f, 0.1f)][Tooltip("Look rotation delta for a render frame is calculated as average from all frames within responsivity time.")]
		private float   _lookResponsivity = 0.020f;
		[SerializeField][Range(0.0f, 1.0f)][Tooltip("How long the last known input is repeated before using default.")]
		private float   _maxRepeatTime = 0.25f;
		[SerializeField][Range(0.0f, 5.0f)][Tooltip("Ignores input for [X] seconds after resuming app.")]
		private float   _ignoreInputOnPause = 2.0f;
		[SerializeField][Range(0.0f, 5.0f)][Tooltip("Maximum extension of ignore input window if a simulation instability is detected after resuming app.")]
		private float   _maxIgnoreInputExtension = 5.0f;
		[SerializeField][Tooltip("Outputs missing inputs to console.")]
		private bool    _logMissingInputs;

		// We need to store current input to compare against previous input (to track actions activation/deactivation). It is also reused if the input for current tick is not available.
		// This is not needed on proxies and will be replicated to input authority only.
		[Networked]
		private ExpertInput   _fixedInput { get; set; }

		private ExpertInput   _renderInput;
		private ExpertInput   _accumulatedInput;
		private ExpertInput   _previousFixedInput;
		private ExpertInput   _previousRenderInput;
		private ExpertInput   _deferActionsInput;
		private bool          _useDeferActionsInput;
		private bool          _updateInterpolationData;
		private Vector2       _partialMoveDirection;
		private float         _partialMoveDirectionSize;
		private Vector2       _accumulatedMoveDirection;
		private float         _accumulatedMoveDirectionSize;
		private SmoothVector2 _smoothLookRotationDelta = new SmoothVector2(256);
		private InputTouches  _inputTouches = new InputTouches();
		private InputTouch    _moveTouch;
		private InputTouch    _lookTouch;
		private bool          _jumpTouch;
		private float         _jumpTime;
		private float         _repeatTime;
		private float         _ignoreTime;
		private float         _ignoreExtension;
		private float         _ignoreRenderTime;
		private float         _lastRenderAlpha;
		private float         _inputPollDeltaTime;
		private int           _lastInputPollFrame;
		private int           _processInputFrame;
		private int           _missingInputsInRow;
		private int           _missingInputsTotal;
		private int           _logMissingInputFromTick;

		// PUBLIC METHODS

		/// <summary>
		/// Check if an action is active in current input. FUN/Render input is resolved automatically.
		/// </summary>
		public bool HasActive(EExpertInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.IsActive(_fixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.IsActive(_renderInput);
			}
		}

		/// <summary>
		/// Check if an action was activated in current input.
		/// In FUN this method compares current fixed input agains previous fixed input.
		/// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasActivated(EExpertInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasActivated(_fixedInput, _previousFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasActivated(_renderInput, _previousRenderInput);
			}
		}

		/// <summary>
		/// Check if an action was deactivated in current input.
		/// In FUN this method compares current fixed input agains previous fixed input.
		/// In Render this method compares current render input against previous render input OR current fixed input (first Render call after FUN).
		/// </summary>
		public bool WasDeactivated(EExpertInputAction action)
		{
			if (Runner.Stage != default)
			{
				CheckFixedAccess(false);
				return action.WasDeactivated(_fixedInput, _previousFixedInput);
			}
			else
			{
				CheckRenderAccess(false);
				return action.WasDeactivated(_renderInput, _previousRenderInput);
			}
		}

		/// <summary>
		/// Updates fixed input. Use after manipulating with fixed input outside.
		/// </summary>
		/// <param name="fixedInput">Input used in fixed update.</param>
		/// <param name="setPreviousInputs">Updates previous fixed input and previous render input.</param>
		public void SetFixedInput(ExpertInput fixedInput, bool setPreviousInputs)
		{
			CheckFixedAccess(true);

			_fixedInput = fixedInput;

			if (setPreviousInputs == true)
			{
				_previousFixedInput  = fixedInput;
				_previousRenderInput = fixedInput;
			}
		}

		/// <summary>
		/// Updates render input. Use after manipulating with render input outside.
		/// </summary>
		/// <param name="renderInput">Input used in render update.</param>
		/// <param name="setPreviousInput">Updates previous render input.</param>
		public void SetRenderInput(ExpertInput renderInput, bool setPreviousInput)
		{
			CheckRenderAccess(false);

			_renderInput = renderInput;

			if (setPreviousInput == true)
			{
				_previousRenderInput = renderInput;
			}
		}

		/// <summary>
		/// Updates accumulated input. Use after manipulating with render/accumulated input outside.
		/// </summary>
		/// <param name="accumulatedInput">Accumulated input from multiple render updates.</param>
		public void SetAccumulatedInput(ExpertInput accumulatedInput)
		{
			CheckRenderAccess(false);

			_accumulatedInput = accumulatedInput;
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			SetDefaults();

			// Wait few seconds before the connection is stable to start tracking missing inputs.
			_logMissingInputFromTick = Runner.Tick + TickRate.Resolve(Runner.Config.Simulation.TickRateSelection).Client * 5;

			_inputTouches.TouchStarted  = OnTouchStarted;
			_inputTouches.TouchFinished = OnTouchFinished;

			if (HasInputAuthority == true)
			{
				// Register local player input polling.
				NetworkEvents networkEvents = Runner.GetComponent<NetworkEvents>();
				networkEvents.OnInput.RemoveListener(OnInput);
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
			_inputTouches.TouchStarted  = null;
			_inputTouches.TouchFinished = null;

			SetDefaults();

			if (runner == null)
				return;

			NetworkEvents networkEvents = runner.GetComponent<NetworkEvents>();
			if (networkEvents != null)
			{
				// Unregister local player input polling.
				networkEvents.OnInput.RemoveListener(OnInput);
			}
		}

		public override void Render()
		{
			// If the following flag is set true, it means that input was polled from OnInput callback, but Actions are deferred to match this Render() and processed next FixedUpdateNetwork().
			// Because alpha values are not set at that time, we need to explicitly update them from Render().
			if (_updateInterpolationData == true)
			{
				_updateInterpolationData = false;

				// Get alpha of the Render() call. Later in FUN we can identify when exactly the action was triggered (render-accurate processing).
				_renderInput.LocalAlpha = Runner.LocalAlpha;

				// Store interpolation data. This is used for render-accurate lag-compensated casts.
				ReflectionUtility.GetInterpolationData(Runner, out _renderInput.InterpolationFromTick, out _renderInput.InterpolationToTick, out _renderInput.InterpolationAlpha);

				// This is first render after input polls, we can safely override the accumulated input.
				_accumulatedInput.LocalAlpha            = _renderInput.LocalAlpha;
				_accumulatedInput.InterpolationAlpha    = _renderInput.InterpolationAlpha;
				_accumulatedInput.InterpolationFromTick = _renderInput.InterpolationFromTick;
				_accumulatedInput.InterpolationToTick   = _renderInput.InterpolationToTick;
			}

			ProcessFrameInput(false);

			if (IsIgnoringInput == true)
			{
				float renderTime = Runner.LocalRenderTime;
				if (renderTime < _ignoreRenderTime)
				{
					// Current render time is lower than previous render time, still adjusting clock after resuming app...
					TryExtendIgnoreInputWindow($"Negative render delta time ({(renderTime - _ignoreRenderTime):F3}s)");
				}

				_ignoreRenderTime = renderTime;
			}

			_lastRenderAlpha = Runner.LocalAlpha;
		}

		// IBeforeAllTicks INTERFACE

		void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
		{
			if (resimulation == false && tickCount >= 10 && IsIgnoringInput == true)
			{
				TryExtendIgnoreInputWindow($"Too many forward ticks ({tickCount})");
			}
		}

		// IBeforeTick INTERFACE

		void IBeforeTick.BeforeTick()
		{
			if (Object == null)
				return;

			Trace(nameof(IBeforeTick.BeforeTick));

			// Store previous fixed input as a base. This will be compared agaisnt new fixed input.
			_previousFixedInput = _fixedInput;

			// Clear all properties which should not propagate from last known input in case of missing input. As an example, following lines will reset look rotation delta.
			// This results to the player not being incorrectly rotated (by using rotation from last known input) in case of missing input on state authority, followed by a correction on the input authority.
			/*
			ExpertInput fixedInput = _fixedInput;
			fixedInput.LookRotationDelta = default;
			_fixedInput = fixedInput;
			*/

			if (Object.InputAuthority == PlayerRef.None)
				return;

			// If this fails, fallback (last known) input will be used as current.
			if (Runner.TryGetInputForPlayer(Object.InputAuthority, out ExpertInput input) == true)
			{
				// New input received, we can store it.
				_fixedInput = input;

				if (Runner.Stage == SimulationStages.Forward)
				{
					// Reset statistics.
					_missingInputsInRow = 0;

					// Reset threshold for repeating inputs.
					_repeatTime = 0.0f;
				}
			}
			else
			{
				if (_ignoreTime > 0.0f)
				{
					// Don't repeat last known input if the ignore time is active (after app resume).
					_fixedInput = default;
				}
				else
				{
					if (Runner.Stage == SimulationStages.Forward)
					{
						// Update statistics.
						++_missingInputsInRow;
						++_missingInputsTotal;

						// Update threshold for repeating inputs.
						_repeatTime += Runner.DeltaTime;

						if (_logMissingInputs == true && Runner.Tick >= _logMissingInputFromTick)
						{
							Debug.LogWarning($"Missing input for {Object.InputAuthority} {Runner.Tick}. In Row: {_missingInputsInRow} Total: {_missingInputsTotal} Repeating Last Known Input: {_repeatTime <= _maxRepeatTime}", gameObject);
						}
					}

					if (_repeatTime > _maxRepeatTime)
					{
						_fixedInput = default;
					}
				}
			}
		}

		// IAfterAllTicks INTERFACE

		void IAfterAllTicks.AfterAllTicks(bool resimulation, int tickCount)
		{
			if (resimulation == true)
				return;

			// All OnInput callbacks were executed, we can reset the temporary flag for polling defer actions input.
			_useDeferActionsInput = default;

			// Input consumed in OnInput callback is always tick-aligned, but the input for this frame is aligned with engine/render time.
			// At this point the accumulated input was consumed up to latest tick time, remains only partial input from latest tick time to render time.
			// The remaining input is stored in render input and accumulated input should be equal.
			_accumulatedInput = _renderInput;

			// The current fixed input will be used as a base for first Render() after FixedUpdateNetwork().
			// This is used to detect changes like NetworkButtons press.
			_previousRenderInput = _fixedInput;

			if (_inputPollDeltaTime > 0.0f)
			{
				// The partial move direction contains input since last engine frame.
				// We need to scale it so it equals to FUN => Render delta time instead of Render => Render.
				float remainingRenderInputRatio = _inputPollDeltaTime / Time.unscaledDeltaTime;

				_partialMoveDirection     *= remainingRenderInputRatio;
				_partialMoveDirectionSize *= remainingRenderInputRatio;
			}

			// Resetting accumulated move direction to values from current frame.
			// Because input for current frame was already processed from OnInput callback, we need to reset accumulation to these values, not zero.
			_accumulatedMoveDirection     = _partialMoveDirection;
			_accumulatedMoveDirectionSize = _partialMoveDirectionSize;

			// Now we can reset last frame render input to defaults.
			_partialMoveDirection     = default;
			_partialMoveDirectionSize = default;
		}

		// MonoBehaviour INTERFACE

		private void LateUpdate()
		{
			// Update ignore time, skip if delta is too big (typically after resuming app).
			if (_ignoreTime > 0.0f)
			{
				float deltaTime = Time.unscaledDeltaTime;
				if (deltaTime < 1.0f)
				{
					_ignoreTime -= deltaTime;
				}
			}
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			if (Application.isMobilePlatform == false)
				return;

			// When app resumes from background, it takes a bit of time to synchronize Fusion simulation with the server.
			// We stop processing input for a short period of time until the simulation is stable - to prevent visual glitches.
			ActivateIgnoreInputWindow();
		}

		// PARTIAL METHODS

		partial void ProcessStandaloneInput(bool isInputPoll);
		partial void ProcessGamepadInput(bool isInputPoll);
		partial void ProcessMobileInput(bool isInputPoll);
		partial void OnTouchStarted(InputTouch touch);
		partial void OnTouchFinished(InputTouch touch);

		// PRIVATE METHODS

		private void ActivateIgnoreInputWindow()
		{
			if (HasInputAuthority == false)
				return;

			// Reset inputs on application pause/resume.
			_renderInput      = default;
			_accumulatedInput = default;

			// Set timeout for ignoring input (polling will be ignored during this period).
			_ignoreTime = _ignoreInputOnPause;

			// Reset ignore input window extension.
			_ignoreExtension = _maxIgnoreInputExtension;

			Debug.LogWarning($"Activating {_ignoreTime:F3}s input ignore window with {_ignoreExtension:F3}s extension.", gameObject);
		}

		private void TryExtendIgnoreInputWindow(string reason)
		{
			float refillTime = 1.0f;

			if (_ignoreExtension <= 0.0f || _ignoreTime <= 0.0f || _ignoreTime > refillTime)
				return;
			if (HasInputAuthority == false)
				return;

			// Detected simulation instability after resuming app, we will extend ignore input window to prevent glitches.

			float consumeExtension = refillTime - _ignoreTime;
			if (consumeExtension > _ignoreExtension)
			{
				consumeExtension = _ignoreExtension;
			}

			_ignoreTime      += consumeExtension;
			_ignoreExtension -= consumeExtension;

			if (consumeExtension > 0.0f)
			{
				Debug.LogWarning($"Detected simulation instability after resuming app. Extending ignore input window by {consumeExtension:F3}s to {_ignoreTime:F3}s with remaining extension {_ignoreExtension:F3}s. Reason:{reason}", gameObject);
			}
		}

		private void OnInput(NetworkRunner runner, NetworkInput networkInput)
		{
			// This method pushes accumulated input to Fusion.
			// It can be executed multiple times within single Unity frame if the rendering speed is slower than Fusion simulation (happens also if there is a performance spike).

			Trace(nameof(OnInput));

			int currentFrame = Time.frameCount;

			bool isFirstPoll = _lastInputPollFrame != currentFrame;
			if (isFirstPoll == true)
			{
				_lastInputPollFrame = currentFrame;
				_inputPollDeltaTime = Time.unscaledDeltaTime;

				if (IsFrameInputProcessed() == false)
				{
					_deferActionsInput = _accumulatedInput;

					ProcessFrameInput(true);
				}
			}

			ExpertInput pollInput = _accumulatedInput;

			if (_inputPollDeltaTime > 0.0001f)
			{
				// At this moment the poll input has render input already accumulated.
				// This "reverts" the poll input to a state before last render input accumulation.
				pollInput.LookRotationDelta -= _renderInput.LookRotationDelta;

				// In the first input poll (within single Unity frame) we want to accumulate only "missing" part to align timing with fixed tick (last Runner.LocalAlpha => 1.0).
				// All subsequent input polls return remaining input which is not yet consumed, but again within alignment limits of fixed ticks (0.0 => 1.0 = current => next).
				float baseRenderAlpha = isFirstPoll == true ? _lastRenderAlpha : 0.0f;

				// Here we calculate delta time between last render time (or last input poll simulation time) and time of the pending simulation tick.
				float pendingTickAlignedDeltaTime = (1.0f - baseRenderAlpha) * Runner.DeltaTime;

				// The full render input look rotation delta is not aligned with ticks, we need to remove delta which is ahead of fixed tick time.
				Vector2 pendingTickAlignedLookRotationDelta = _renderInput.LookRotationDelta * Mathf.Clamp01(pendingTickAlignedDeltaTime / _inputPollDeltaTime);

				// Accumulate look rotation delta up to aligned tick time.
				pollInput.LookRotationDelta += pendingTickAlignedLookRotationDelta;

				// Consume same look rotation delta from render input.
				_renderInput.LookRotationDelta -= pendingTickAlignedLookRotationDelta;

				// Decrease remaining input poll delta time by the partial delta time consumed by accumulation.
				_inputPollDeltaTime = Mathf.Max(0.0f, _inputPollDeltaTime - pendingTickAlignedDeltaTime);

				// Accumulated input is now consumed and should equal to remaining render input (after tick-alignment).
				// This will be fully/partially consumed by following OnInput call(s) or next frame.
				_accumulatedInput.LookRotationDelta = _renderInput.LookRotationDelta;
			}
			else
			{
				// Input poll delta time is too small, we consume whole input.
				_accumulatedInput.LookRotationDelta = default;
				_renderInput.LookRotationDelta      = default;
				_inputPollDeltaTime                 = default;
			}

			if (_useDeferActionsInput == true)
			{
				// An action was triggered but it should be processed by the first fixed simulation tick after Render().
				// Instead of polling the accumulated input, we replace actions by accumulated input before the action was triggered.
				pollInput.Actions               = _deferActionsInput.Actions;
				pollInput.LocalAlpha            = _deferActionsInput.LocalAlpha;
				pollInput.InterpolationAlpha    = _deferActionsInput.InterpolationAlpha;
				pollInput.InterpolationFromTick = _deferActionsInput.InterpolationFromTick;
				pollInput.InterpolationToTick   = _deferActionsInput.InterpolationToTick;
			}

			// Don't set the input if the ignore time is active.
			if (_ignoreTime > 0.0f)
				return;

			networkInput.Set(pollInput);
		}

		private bool IsFrameInputProcessed() => _processInputFrame == Time.frameCount;

		private void ProcessFrameInput(bool isInputPoll)
		{
			// Collect input from devices.
			// Can be executed multiple times between FixedUpdateNetwork() calls because of faster rendering speed.
			// However the input is processed only once per frame.

			int currentFrame = Time.frameCount;
			if (currentFrame == _processInputFrame)
				return;

			_processInputFrame = currentFrame;

			// Store last render input as a base to current render input.
			_previousRenderInput = _renderInput;

			// Reset input for current frame to default.
			_renderInput = default;

			// Only input authority is tracking render input.
			if (HasInputAuthority == false)
				return;

			Trace(nameof(ProcessFrameInput));

			if (Application.isMobilePlatform == false || Application.isEditor == true)
			{
				// Input is tracked only if the cursor is locked.
				if (Cursor.lockState != CursorLockMode.Locked)
				{
					_accumulatedInput = default;
					return;
				}
			}

			// Don't process the input if the ignore time is active.
			if (_ignoreTime > 0.0f)
			{
				_accumulatedInput = default;
				return;
			}

			// Storing the accumulated input for reference.
			ExpertInput previousAccumulatedInput = _accumulatedInput;

			if (Application.isMobilePlatform == true && Application.isEditor == false)
			{
				_inputTouches.Update();

				ProcessMobileInput(isInputPoll);
			}
			else
			{
				ProcessStandaloneInput(isInputPoll);
			}

			ProcessGamepadInput(isInputPoll);

			AccumulateRenderInput();

			if (isInputPoll == true)
			{
				// Check actions that were triggered in this frame and should be deferred - and processed by the first fixed simulation tick after Render().

				for (int i = 0; i < InterpolationDataActions.Length; ++i)
				{
					if (InterpolationDataActions[i].WasActivated(_renderInput, previousAccumulatedInput) == true)
					{
						// Actions that require interpolation data are always deferred.
						_useDeferActionsInput = true;

						// We cannot set alpha value because it is not calculated yet. Postponing to Render().
						_updateInterpolationData = true;

						break;
					}
				}

				if (_useDeferActionsInput == false)
				{
					for (int i = 0; i < DeferredInputActions.Length; ++i)
					{
						if (DeferredInputActions[i].WasActivated(_renderInput, previousAccumulatedInput) == true)
						{
							_useDeferActionsInput = true;
							break;
						}
					}
				}
			}
			else
			{
				// Actions were triggered from Render() in this frame.
				// Interpolation data is correctly calculated and can be directly written to input.

				for (int i = 0; i < InterpolationDataActions.Length; ++i)
				{
					if (InterpolationDataActions[i].WasActivated(_renderInput, previousAccumulatedInput) == true)
					{
						_renderInput.LocalAlpha = Runner.LocalAlpha;
						ReflectionUtility.GetInterpolationData(Runner, out _renderInput.InterpolationFromTick, out _renderInput.InterpolationToTick, out _renderInput.InterpolationAlpha);

						_accumulatedInput.LocalAlpha            = _renderInput.LocalAlpha;
						_accumulatedInput.InterpolationAlpha    = _renderInput.InterpolationAlpha;
						_accumulatedInput.InterpolationFromTick = _renderInput.InterpolationFromTick;
						_accumulatedInput.InterpolationToTick   = _renderInput.InterpolationToTick;

						break;
					}
				}
			}
		}

		private void AccumulateRenderInput()
		{
			// We don't accumulate render move direction directly, instead we accumulate the value multiplied by delta time, the result is then divided by total time accumulated.
			// This approach correctly reflects full throttle in last frame with very fast rendering and is more consistent with fixed simulation.

			_partialMoveDirectionSize = Time.unscaledDeltaTime;
			_partialMoveDirection     = _renderInput.MoveDirection * _partialMoveDirectionSize;

			// In other words:
			// Move direction accumulation is a special case. Let's say simulation runs 30Hz (33.333ms delta time) and render runs 300Hz (3.333ms delta time).
			// If the player hits a key to run forward in last frame before fixed tick, the KCC will move in render by (velocity * 0.003333f).
			// Treating this input the same way for next fixed tick results in KCC moving by (velocity * 0.03333f) - 10x more.
			// Following accumulation proportionally scales move direction so it reflects frames in which input was active.
			// This way the next fixed tick will correspond more accurately to what happened in predicted render.

			_accumulatedMoveDirectionSize += _partialMoveDirectionSize;
			_accumulatedMoveDirection     += _partialMoveDirection;

			// Accumulate input for the OnInput() call, the result represents sum of inputs for all render frames since last fixed tick.
			_accumulatedInput.Actions            = new NetworkButtons(_accumulatedInput.Actions.Bits | _renderInput.Actions.Bits);
			_accumulatedInput.MoveDirection      = _accumulatedMoveDirection / _accumulatedMoveDirectionSize;
			_accumulatedInput.LookRotationDelta += _renderInput.LookRotationDelta;

			// Accumulate your own properties here.
		}

		private Vector2 GetSmoothLookRotationDelta(Vector2 lookRotationDelta, Vector2 lookRotationSensitivity)
		{
			lookRotationDelta *= lookRotationSensitivity;

			// If the look rotation responsivity is enabled, calculate average delta instead.
			if (_lookResponsivity > 0.0f)
			{
				// Kill any rotation in opposite direction for instant direction flip.
				_smoothLookRotationDelta.FilterValues(lookRotationDelta.x < 0.0f, lookRotationDelta.x > 0.0f, lookRotationDelta.y < 0.0f, lookRotationDelta.y > 0.0f);

				// Add or update value for current frame.
				_smoothLookRotationDelta.AddValue(Time.frameCount, Time.unscaledDeltaTime, lookRotationDelta);

				// Calculate smooth look rotation delta.
				lookRotationDelta = _smoothLookRotationDelta.CalculateSmoothValue(_lookResponsivity, Time.unscaledDeltaTime);
			}

			return lookRotationDelta;
		}

		private void SetDefaults()
		{
			_fixedInput              = default;
			_renderInput             = default;
			_accumulatedInput        = default;
			_previousFixedInput      = default;
			_previousRenderInput     = default;
			_deferActionsInput       = default;
			_useDeferActionsInput    = default;
			_updateInterpolationData = default;
			_moveTouch               = default;
			_lookTouch               = default;
			_repeatTime              = default;
			_ignoreTime              = default;
			_ignoreExtension         = default;
			_ignoreRenderTime        = default;
			_lastRenderAlpha         = default;
			_inputPollDeltaTime      = default;
			_lastInputPollFrame      = default;
			_processInputFrame       = default;
			_missingInputsTotal      = default;
			_missingInputsInRow      = default;

			_smoothLookRotationDelta.ClearValues();
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		private void CheckFixedAccess(bool checkStage)
		{
			if (checkStage == true && Runner.Stage == default)
			{
				throw new InvalidOperationException("This call should be executed from FixedUpdateNetwork!");
			}

			if (Runner.Stage != default && IsProxy == true)
			{
				throw new InvalidOperationException("Fixed input is available only on State & Input authority!");
			}
		}

		[System.Diagnostics.Conditional("UNITY_EDITOR")]
		[System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
		private void CheckRenderAccess(bool checkStage)
		{
			if (checkStage == true && Runner.Stage != default)
			{
				throw new InvalidOperationException("This call should be executed outside of FixedUpdateNetwork!");
			}

			if (Runner.Stage == default && HasInputAuthority == false)
			{
				throw new InvalidOperationException("Render and accumulated inputs are available only on Input authority!");
			}
		}

		[HideInCallstack]
		[System.Diagnostics.Conditional(KCC.TRACING_SCRIPT_DEFINE)]
		private void Trace(params object[] messages)
		{
			KCCUtility.Trace<PlayerInput>(this, messages);
		}
	}
}
