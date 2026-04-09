namespace Example.StageObject
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	// 1. Definition of the stage type - it must be an interface to use it on processors.
	//================================================================================
	public interface IDamageBoost : IKCCStage<DamageBoost>
	{
		// Notice we are using DamageBoost as the stage object type.
	}

	// 2. Definition of the stage object type.
	//================================================================================
	// This is a special stage object type which can be passed as a parameter to KCC.ExecuteStage().
	// The stage object itself must implement stage interface to work correctly.
	public sealed class DamageBoost : IDamageBoost, IBeforeStage, IAfterStage
	{
		// This property represents maximum damage boost from all sources (processors implementing IDamageBoost interface).
		public float MaxValue => _maxValue;

		private float _maxValue;

		// This method is expected to be used from outside (other processors implementing IDamageBoost interface).
		public void ProcessDamageBoost(float damageBoost)
		{
			_maxValue = Mathf.Max(_maxValue, damageBoost);
		}

		// Explicit implementation hides the Execute() method from DamageBoost public API.
		void IKCCStage<DamageBoost>.Execute(DamageBoost stage, KCC kcc, KCCData data)
		{
			// Execution also happens on stage object itself (stage == this).
			// This method is executed after all processors and before post-processes.
		}

		// IBeforeStage is optional and executed on stage objects only.
		// It is typically used to reset members to default values before stage is executed.
		void IBeforeStage.BeforeStage(KCC kcc, KCCData data)
		{
			// This callback is executed first, before any processor.
			// In this case we initialize _maxValue to zero.
			_maxValue = default;
		}

		// IAfterStage is optional and executed on stage objects only.
		// It is typically used to iterate over collected information and calculate results on the end of the stage.
		void IAfterStage.AfterStage(KCC kcc, KCCData data)
		{
			// This callback is executed last, after all processors, stage object and post-processes.
			// In this case we clamp the _maxValue between 0.0f and 5.0f.
			_maxValue = Mathf.Clamp(_maxValue, 0.0f, 5.0f);
		}
	}

	// 3. Example processor implementing the IDamageBoost stage.
	//================================================================================
	// Any processor providing damage boost to a player can implement IDamageBoost interface.
	public class DamageBoostArea : KCCProcessor, IDamageBoost
	{
		public float DamageBoost = 4.0f;

		public void Execute(DamageBoost stage, KCC kcc, KCCData data)
		{
			// Notice we get stage object instance (of type DamageBoost) as parameter.

			// Simply passing the value to the stage object.
			stage.ProcessDamageBoost(DamageBoost);
		}
	}

	// 4. Usage of the stage object, execution of the stage, application of result
	//================================================================================
	public class Player : NetworkBehaviour
	{
		public KCC KCC;

		// The stage is used like a player "attribute".
		private DamageBoost _damageBoost = new DamageBoost();

		public override void FixedUpdateNetwork()
		{
			// Use all benefits of stage execution:
			// 1. Simple iteration over registered processors.
			// 2. Processors sorted by priority.
			// 3. Processors suppressing.
			// 4. Zero component lookups.
			// 5. Zero physics queries.
			KCC.ExecuteStage(_damageBoost);

			float damageMultiplier = _damageBoost.MaxValue;

			// Firing from weapon...
			// The damage multiplier can be combined with base projectile damage.
		}
	}
}
