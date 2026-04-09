namespace Example.ExpertMovement
{
	using UnityEngine;
	using UnityEngine.InputSystem;
	using Fusion.Addons.KCC;

	public sealed partial class ExpertPlayerInput
	{
		partial void ProcessStandaloneInput(bool isInputPoll)
		{
			// Always use KeyControl.isPressed, Input.GetMouseButton() and Input.GetKey().
			// Never use KeyControl.wasPressedThisFrame, Input.GetMouseButtonDown() or Input.GetKeyDown() otherwise the action might be lost.

			Vector2 moveDirection     = Vector2.zero;
			Vector2 lookRotationDelta = Vector2.zero;

			Mouse mouse = Mouse.current;
			if (mouse != null)
			{
				Vector2 mouseDelta = mouse.delta.ReadValue();

				lookRotationDelta = GetSmoothLookRotationDelta(new Vector2(-mouseDelta.y, mouseDelta.x), _standaloneLookSensitivity);

				_renderInput.LMB = mouse.leftButton.isPressed;
				_renderInput.RMB = mouse.rightButton.isPressed;
				_renderInput.MMB = mouse.middleButton.isPressed;
			}

			Keyboard keyboard = Keyboard.current;
			if (keyboard != null)
			{
				if (keyboard.mKey.isPressed == true && keyboard.leftCtrlKey.isPressed == true && keyboard.leftShiftKey.isPressed == true)
				{
					// Simulate application pause/resume.
					ActivateIgnoreInputWindow();
				}

				if (keyboard.wKey.isPressed == true) { moveDirection += Vector2.up;    }
				if (keyboard.sKey.isPressed == true) { moveDirection += Vector2.down;  }
				if (keyboard.aKey.isPressed == true) { moveDirection += Vector2.left;  }
				if (keyboard.dKey.isPressed == true) { moveDirection += Vector2.right; }

				if (moveDirection.IsZero() == false)
				{
					moveDirection.Normalize();
				}

				// Camera smoothness testing => side walk + rotation with constant speed. You can safely remove it.
				if (keyboard.qKey.isPressed == true)
				{
					moveDirection = Vector2.left;
					lookRotationDelta = new Vector2(0.0f, 60.0f * Time.deltaTime);
				}

				// Camera smoothness testing => side walk + rotation with constant speed. You can safely remove it.
				if (keyboard.eKey.isPressed == true)
				{
					moveDirection = Vector2.right;
					lookRotationDelta = new Vector2(0.0f, -60.0f * Time.deltaTime);
				}

				_renderInput.Jump   = keyboard.spaceKey.isPressed;
				_renderInput.Dash   = keyboard.tabKey.isPressed;
				_renderInput.Sprint = keyboard.leftShiftKey.isPressed;

				if (HasInputAuthority == true)
				{
					// Here we can use KeyControl.wasPressedThisFrame / Input.GetKeyDown() because it is not part of input structure and we send actions through RPC.
					if (keyboard.numpadPlusKey.wasPressedThisFrame  == true) { GetComponent<ExpertPlayer>().ToggleSpeedRPC(1);  }
					if (keyboard.numpadMinusKey.wasPressedThisFrame == true) { GetComponent<ExpertPlayer>().ToggleSpeedRPC(-1); }
				}
			}

			_renderInput.MoveDirection     = moveDirection;
			_renderInput.LookRotationDelta = lookRotationDelta;

			// If you add a new property to the input, it must be also accumulated.
			// Please check ExpertPlayerInput.AccumulateRenderInput().
		}
	}
}
