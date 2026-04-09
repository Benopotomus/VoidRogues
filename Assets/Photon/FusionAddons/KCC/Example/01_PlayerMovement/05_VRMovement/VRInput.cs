using UnityEngine;
using Fusion;

namespace Example.VRMovement
{
	/// <summary>
	/// Input structure polled by Fusion. This is sent over network and processed by server, keep it optimized and remove unused data.
	/// </summary>
	public struct VRInput : INetworkInput
	{
		public Vector2        MoveDirection;
		public Vector2        LookRotationDelta;
		public Vector3        HeadPosition;
		public Quaternion     HeadRotation;
		public Vector3        LeftHandPosition;
		public Quaternion     LeftHandRotation;
		public Vector3        RightHandPosition;
		public Quaternion     RightHandRotation;
		public NetworkButtons Actions;

		public const int LT_BUTTON = 0;
		public const int RT_BUTTON = 1;
	}
}
