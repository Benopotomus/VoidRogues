namespace Example.Forces
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - applying dynamic impulse to a KCC when it starts interacting with the processor.
	/// The processor can be registered manually (via KCC.AddModifier() call) or automatically based on collisions.
	/// </summary>
	public sealed class DynamicImpulseProcessor : KCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Vector3 _impulse;
		[SerializeField]
		private LimitKinematicVelocityProcessor _limitKinematicVelocityProcessor;

		// KCCProcessor INTERFACE

		public override void OnEnter(KCC kcc, KCCData data)
		{
			if (_impulse.IsZero() == true)
				return;

			Vector3 rotatedImpulse = transform.rotation * _impulse;

			// Clear kinematic velocity entirely
			kcc.SetKinematicVelocity(Vector3.zero);

			// Clear dynamic velocity proportionaly to impulse direction
			kcc.SetDynamicVelocity(data.DynamicVelocity - Vector3.Scale(data.DynamicVelocity, rotatedImpulse.normalized));

			// Explicitly set current position, this kills any remaining movement (CCD might be active)
			kcc.SetPosition(data.TargetPosition);

			// Add impulse
			kcc.AddExternalImpulse(rotatedImpulse);

			// Add special processor which prevents kinematic movement
			kcc.AddModifier(_limitKinematicVelocityProcessor);
		}
	}
}
