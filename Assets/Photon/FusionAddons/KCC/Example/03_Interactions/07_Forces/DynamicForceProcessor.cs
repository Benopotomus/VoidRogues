namespace Example.Forces
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - applying dynamic force every tick/frame to all KCCs interacting with the processor.
	/// The processor can be registered manually (via KCC.AddModifier() call) or automatically based on collisions.
	/// </summary>
	public sealed class DynamicForceProcessor : KCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Vector3 _force;

		// KCCProcessor INTERFACE

		public override void OnEnter(KCC kcc, KCCData data)
		{
			ApplyForce(kcc, data);
		}

		public override void OnStay(KCC kcc, KCCData data)
		{
			ApplyForce(kcc, data);
		}

		// PRIVATE METHODS

		private void ApplyForce(KCC kcc, KCCData data)
		{
			if (_force.IsZero() == true)
				return;

			kcc.AddExternalForce(transform.rotation * _force);
		}
	}
}
