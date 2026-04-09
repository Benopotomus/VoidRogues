namespace Example.JumpPad
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - applying dynamic impulse to get KCC to a specific destination.
	/// </summary>
	public sealed class JumpPad : KCCProcessor, ISetDynamicVelocity, ISetKinematicVelocity
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform _destination;

		// KCCProcessor INTERFACE

		public override float GetPriority(KCC kcc) => float.MaxValue;

		public override void OnEnter(KCC kcc, KCCData data)
		{
			// Clear kinematic and dynamic velocity entirely
			kcc.SetKinematicVelocity(Vector3.zero);
			kcc.SetDynamicVelocity(Vector3.zero);

			// Explicitly set current position, this kills any remaining movement (CCD might be active)
			kcc.SetPosition(data.TargetPosition);

			// Force un-ground KCC
			data.IsGrounded = false;

			// Calculate how long it takes to reach apex
			Vector3 offset   = _destination.position - data.TargetPosition;
			float   apexTime = Mathf.Sqrt(-2.0f * offset.y / data.Gravity.y) + data.UpdateDeltaTime * 0.5f;

			// Calculate initial velocity
			Vector3 velocity = offset.OnlyXZ() / apexTime - data.Gravity.OnlyY() * apexTime;

			kcc.SetDynamicVelocity(velocity);
		}

		// IKCCInteractionProvider INTERFACE

		public override bool CanStartInteraction(KCC kcc, KCCData data)
		{
			// The interaction starts only if we are under destination level.
			return data.TargetPosition.y < _destination.position.y;
		}

		public override bool CanStopInteraction(KCC kcc, KCCData data)
		{
			// The interaction will be stopped when grounded.
			return data.IsGrounded == true && data.WasGrounded == true;
		}

		// ISetDynamicVelocity INTERFACE

		public void Execute(ISetDynamicVelocity stage, KCC kcc, KCCData data)
		{
			// Applying gravity.
			data.DynamicVelocity += data.Gravity * kcc.FixedData.DeltaTime;

			// All transient properties are consumed.
			data.ClearTransientProperties();

			// Suppress all other processors.
			kcc.SuppressProcessors<IKCCProcessor>();
		}

		// ISetKinematicVelocity INTERFACE

		public void Execute(ISetKinematicVelocity stage, KCC kcc, KCCData data)
		{
			// Suppress kinematic movement completely.
			data.KinematicVelocity = default;

			// Suppress all other processors.
			kcc.SuppressProcessors<IKCCProcessor>();
		}
	}
}
