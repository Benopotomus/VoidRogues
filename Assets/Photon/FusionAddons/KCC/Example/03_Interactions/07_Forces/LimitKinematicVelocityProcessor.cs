namespace Example.Forces
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - clears kinematic velocity if the real speed is greater than threshold.
	/// Effectively this stops any kinematic movement if there is a high-velocity source of dynamic movement.
	/// This processor removes itself from owner KCC when it is done (real speed is below threshold).
	/// </summary>
	public sealed class LimitKinematicVelocityProcessor : KCCProcessor, IPrepareData
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _clearKinematicVelocityIfAboveSpeed;

		// KCCProcessor INTERFACE

		// This processor has lowest priority to execute last.
		public override float GetPriority(KCC kcc) => float.MinValue;

		public override void OnEnter(KCC kcc, KCCData data)
		{
			TryClearVelocity(data);
		}

		public override void OnStay(KCC kcc, KCCData data)
		{
			if (_clearKinematicVelocityIfAboveSpeed > 0.0f && data.RealSpeed >= _clearKinematicVelocityIfAboveSpeed)
				return;

			// Overall speed is below threshold, we can remove self from the KCC.
			kcc.RemoveModifier(this);
		}

		// IPrepareData INTERFACE

		public void Execute(PrepareData stage, KCC kcc, KCCData data)
		{
			TryClearVelocity(data);
		}

		// PRIVATE METHODS

		private void TryClearVelocity(KCCData data)
		{
			if (_clearKinematicVelocityIfAboveSpeed > 0.0f && data.RealSpeed >= _clearKinematicVelocityIfAboveSpeed)
			{
				data.KinematicVelocity = Vector3.zero;
			}
		}
	}
}
