namespace Example.ExpertMovement
{
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion.Addons.KCC;

	public sealed partial class ExpertPlayerInput
	{
		partial void ProcessGamepadInput(bool isInputPoll)
		{
			Gamepad gamepad = Gamepad.current;
			if (gamepad == null)
				return;

			// Notice we propagate the gamepad input additively (setting vectors only if the value is non-zero, using '|=' instead of '=').
			// This approach won't override input which might be already set from keyboard and mouse.

			Vector2 moveDirection = gamepad.leftStick.ReadValue();
			if (moveDirection.IsAlmostZero(0.1f) == false)
			{
				_renderInput.MoveDirection = moveDirection;
			}

			Vector2 lookRotationDelta = gamepad.rightStick.ReadValue();
			if (lookRotationDelta.IsAlmostZero() == false)
			{
				lookRotationDelta = new Vector2(-lookRotationDelta.y, lookRotationDelta.x);
				lookRotationDelta = GetSmoothLookRotationDelta(lookRotationDelta, _gamepadLookSensitivity);

				_renderInput.LookRotationDelta = lookRotationDelta;
			}

			_renderInput.Jump   |= gamepad.rightTrigger.isPressed;
			_renderInput.Sprint |= gamepad.leftTrigger.isPressed;
			_renderInput.Dash   |= gamepad.aButton.isPressed;

			// If you add a new property to the input, it must be also accumulated.
			// Please check ExpertPlayerInput.AccumulateRenderInput().
		}
	}
}
