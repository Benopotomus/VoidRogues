namespace Example.VRMovement
{
	using System.Collections.Generic;
	using UnityEngine;
	using UnityEngine.XR;

	using XRInputDevice  = UnityEngine.XR.InputDevice;
	using XRCommonUsages = UnityEngine.XR.CommonUsages;

	public struct VRPose
	{
		// PUBLIC MEMBERS

		public Vector3    HeadPosition;
		public Quaternion HeadRotation;
		public Vector3    LeftHandPosition;
		public Quaternion LeftHandRotation;
		public Vector3    RightHandPosition;
		public Quaternion RightHandRotation;

		// PRIVATE MEMBERS

		private static readonly List<XRInputSubsystem> _inputSubsystems = new List<XRInputSubsystem>();

		// PUBLIC METHODS

		public static void CheckTrackingOriginMode()
		{
			#if UNITY_6000_0_OR_NEWER
			SubsystemManager.GetSubsystems(_inputSubsystems);
			#else
			SubsystemManager.GetInstances(_inputSubsystems);
			#endif

			for (int i = 0, count = _inputSubsystems.Count; i < count; ++i)
			{
				XRInputSubsystem inputSubsystem = _inputSubsystems[i];
				if (inputSubsystem != null && inputSubsystem.GetTrackingOriginMode() != TrackingOriginModeFlags.Floor)
				{
					inputSubsystem.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
				}
			}
		}

		public static VRPose Get()
		{
			CheckTrackingOriginMode();

			VRPose pose = new VRPose();

			XRInputDevice head      = InputDevices.GetDeviceAtXRNode(XRNode.Head);
			XRInputDevice leftHand  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
			XRInputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

			if (     head.isValid == true &&      head.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3      headPosition) == true) { pose.HeadPosition      =      headPosition; }
			if ( leftHand.isValid == true &&  leftHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3  leftHandPosition) == true) { pose.LeftHandPosition  =  leftHandPosition; }
			if (rightHand.isValid == true && rightHand.TryGetFeatureValue(XRCommonUsages.devicePosition, out Vector3 rightHandPosition) == true) { pose.RightHandPosition = rightHandPosition; }

			if (     head.isValid == true &&      head.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion      headRotation) == true) { pose.HeadRotation      =      headRotation; }
			if ( leftHand.isValid == true &&  leftHand.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion  leftHandRotation) == true) { pose.LeftHandRotation  =  leftHandRotation; }
			if (rightHand.isValid == true && rightHand.TryGetFeatureValue(XRCommonUsages.deviceRotation, out Quaternion rightHandRotation) == true) { pose.RightHandRotation = rightHandRotation; }

			return pose;
		}
	}
}
