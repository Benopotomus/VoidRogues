namespace Example.ExpertMovement
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	public sealed partial class ExpertPlayerInput
	{
		partial void ProcessMobileInput(bool isInputPoll)
		{
			bool    sprint            = false;
			Vector2 moveDirection     = Vector2.zero;
			Vector2 lookRotationDelta = Vector2.zero;

			if (_lookTouch != null && _lookTouch.IsActive == true)
			{
				lookRotationDelta = new Vector2(-_lookTouch.Delta.Position.y, _lookTouch.Delta.Position.x);
			}

			lookRotationDelta = GetSmoothLookRotationDelta(lookRotationDelta, _mobileLookSensitivity);

			if (_moveTouch != null && _moveTouch.IsActive == true && _moveTouch.GetDelta().Position.IsZero() == false)
			{
				float screenSizeFactor = 8.0f / Mathf.Min(Screen.width, Screen.height);

				moveDirection = new Vector2(_moveTouch.GetDelta().Position.x, _moveTouch.GetDelta().Position.y) * screenSizeFactor;
				if (moveDirection.sqrMagnitude > 1.0f)
				{
					moveDirection.Normalize();
				}

				if (_moveTouch.GetDelta().Position.magnitude > Screen.height * 0.1f)
				{
					sprint = true;
				}
			}

			_renderInput.Jump              = _jumpTouch;
			_renderInput.Sprint            = sprint;
			_renderInput.MoveDirection     = moveDirection;
			_renderInput.LookRotationDelta = lookRotationDelta;

			// If you add a new property to the input, it must be also accumulated.
			// Please check ExpertPlayerInput.AccumulateRenderInput().
		}

		partial void OnTouchStarted(InputTouch touch)
		{
			if (_moveTouch == null && touch.Start.Position.x < Screen.width * 0.5f)
			{
				_moveTouch = touch;
			}

			if (_lookTouch == null && touch.Start.Position.x > Screen.width * 0.5f)
			{
				_lookTouch = touch;
				_jumpTouch = default;

				if (_jumpTime > Time.realtimeSinceStartup - 0.25f)
				{
					_jumpTouch = true;
				}

				_jumpTime = Time.realtimeSinceStartup;
			}
		}

		partial void OnTouchFinished(InputTouch touch)
		{
			if (_moveTouch == touch) { _moveTouch = default; }
			if (_lookTouch == touch) { _lookTouch = default; _jumpTouch = default; }
		}
	}
}
