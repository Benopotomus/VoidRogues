namespace Example.SpeedModifiers
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - multiplying kinematic speed based on player count interacting with the processor.
	/// This processor contains networked data and requires spawn => it cannot be added as prefab modifier to the KCC.
	/// </summary>
	public sealed class GroupSpeedProcessor : NetworkKCCProcessor, ISetKinematicSpeed, ISpeedModifier
	{
		// PRIVATE MEMBERS

		// The multiplier needs to be networked to support rollback.
		[Networked]
		private int _speedMultiplier { get; set; }

		// NetworkKCCProcessor INTERFACE

		// Like other processors in the same category (identified by ISpeedModifier interface), the priority equals speed multiplier.
		public override float GetPriority(KCC kcc) => _speedMultiplier;

		public override void OnEnter(KCC kcc, KCCData data)
		{
			// Speed multiplier is updated only in fixed updates to simplify things and not to mess with render prediction.
			// This is generally enough and less error prone (in most cases you don't need instant feedback for render).
			// The value in render udates will be the same as the value in latest fixed update.

			if (kcc.IsInFixedUpdate == true)
			{
				_speedMultiplier = Mathf.Min(_speedMultiplier + 1, 8);
			}
		}

		public override void OnExit(KCC kcc, KCCData data)
		{
			if (kcc.IsInFixedUpdate == true)
			{
				_speedMultiplier = Mathf.Max(0, _speedMultiplier - 1);
			}
		}

		// ISetKinematicSpeed INTERFACE

		public void Execute(ISetKinematicSpeed stage, KCC kcc, KCCData data)
		{
			// Note on execution priority:
			// Base value for KCCData.KinematicSpeed is set by EnvironmentProcessor before executing ISetKinematicSpeed stage so it is safe to set any priority including float.MaxValue.
			// Because we have various speed modifiers in this project, we need some priority rules and all processors have to respect them.
			// Otherwise a processor with higher priority and lower speed bonus could suppress another processor with lower priority but greater speed bonus, which could be wrong.

			data.KinematicSpeed *= _speedMultiplier;

			// Suppress all other processors in same category (identified by the ISpeedModifier interface) with lower priority.
			// If there is a ISpeedModifier processor executed with higher priority (before this processor) following same rules, this method won't be executed.
			// This mechanism prevents stacking multipliers (potential gameplay hacks) in a clean way.
			kcc.SuppressProcessors<ISpeedModifier>();
		}
	}
}
