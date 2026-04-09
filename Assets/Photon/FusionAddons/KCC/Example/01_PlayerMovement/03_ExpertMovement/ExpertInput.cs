namespace Example.ExpertMovement
{
	using UnityEngine;
	using Fusion;

	public enum EExpertInputAction
	{
		LMB    = 0,
		RMB    = 1,
		MMB    = 2,
		Jump   = 3,
		Dash   = 4,
		Sprint = 5,
	}

	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct ExpertInput : INetworkInput
	{
		// PUBLIC MEMBERS

		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Actions;
		public float          LocalAlpha;            // This value is used for render-accurate actions. Valid range is 0.0 - 1.0 and represents position of the time between current and last fixed tick.
		public float          InterpolationAlpha;    // This value is used for render-accurate lag compensated casts.
		public int            InterpolationFromTick; // This value is used for render-accurate lag compensated casts.
		public int            InterpolationToTick;   // This value is used for render-accurate lag compensated casts.

		public bool LMB    { get { return Actions.IsSet(EExpertInputAction.LMB);    } set { Actions.Set(EExpertInputAction.LMB,    value); } }
		public bool RMB    { get { return Actions.IsSet(EExpertInputAction.RMB);    } set { Actions.Set(EExpertInputAction.RMB,    value); } }
		public bool MMB    { get { return Actions.IsSet(EExpertInputAction.MMB);    } set { Actions.Set(EExpertInputAction.MMB,    value); } }
		public bool Jump   { get { return Actions.IsSet(EExpertInputAction.Jump);   } set { Actions.Set(EExpertInputAction.Jump,   value); } }
		public bool Dash   { get { return Actions.IsSet(EExpertInputAction.Dash);   } set { Actions.Set(EExpertInputAction.Dash,   value); } }
		public bool Sprint { get { return Actions.IsSet(EExpertInputAction.Sprint); } set { Actions.Set(EExpertInputAction.Sprint, value); } }
	}

	public static class ExpertInputActionExtensions
	{
		// PUBLIC METHODS

		public static bool IsActive(this EExpertInputAction action, ExpertInput input)
		{
			return input.Actions.IsSet(action) == true;
		}

		public static bool WasActivated(this EExpertInputAction action, ExpertInput currentInput, ExpertInput previousInput)
		{
			return currentInput.Actions.IsSet(action) == true && previousInput.Actions.IsSet(action) == false;
		}

		public static bool WasDeactivated(this EExpertInputAction action, ExpertInput currentInput, ExpertInput previousInput)
		{
			return currentInput.Actions.IsSet(action) == false && previousInput.Actions.IsSet(action) == true;
		}
	}
}
