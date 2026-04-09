using UnityEngine;
using Fusion;

namespace Example.BasicMovement
{
	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct BasicInput : INetworkInput
	{
		public Vector2     MoveDirection;
		public Vector2     LookRotationDelta;
		public NetworkBool Jump;
	}
}
