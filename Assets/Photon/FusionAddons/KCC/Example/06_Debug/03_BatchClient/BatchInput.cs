namespace Example.BatchClient
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server.
	/// </summary>
	public struct BatchInput : INetworkInput
	{
		public Vector3     MoveDirection;
		public Vector2     LookRotationDelta;
		public NetworkBool Jump;
	}
}
