using UnityEngine;
using Fusion;

namespace Example.AdvancedMovement
{
	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct AdvancedInput : INetworkInput
	{
		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public NetworkButtons Actions;

		public const int JUMP_BUTTON   = 0;
		public const int SPRINT_BUTTON = 1;
	}
}
