namespace Example.SpeedModifiers
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - multiplying kinematic speed of the KCC.
	/// </summary>
	public sealed class SpeedMultiplierProcessor : KCCProcessor, ISetKinematicSpeed, ISpeedModifier
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private float _speedMultiplier = 1.0f;

		// KCCProcessor INTERFACE

		// Like other processors in the same category (identified by ISpeedModifier interface), the priority equals speed multiplier.
		public override float GetPriority(KCC kcc) => _speedMultiplier;

		// ISetKinematicSpeed INTERFACE

		public void Execute(ISetKinematicSpeed stage, KCC kcc, KCCData data)
		{
			// Note on execution priority:
			// Base value for KCCData.KinematicSpeed is set by EnvironmentProcessor before executing ISetKinematicSpeed stage so it is safe to set any priority including float.MaxValue.
			// Because we have various speed modifiers in this project, we need some priority rules and all processors have to respect them.
			// Otherwise a processor with higher priority and lower speed bonus could suppress another processor with lower priority but greater speed bonus, which could be wrong.

			// Apply multiplier.
			data.KinematicSpeed *= _speedMultiplier;

			// Suppress all other processors in same category (identified by the ISpeedModifier interface) with lower priority.
			// If there is a ISpeedModifier processor executed with higher priority (before this processor) following same rules, this method won't be executed.
			// This mechanism prevents stacking multipliers (potential gameplay hacks) in a clean way.
			kcc.SuppressProcessors<ISpeedModifier>();
		}
	}
}
