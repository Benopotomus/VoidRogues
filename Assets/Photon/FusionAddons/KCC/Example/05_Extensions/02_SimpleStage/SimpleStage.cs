namespace Example.SimpleStage
{
	using Fusion;
	using Fusion.Addons.KCC;

	// 1. Definition of a custom stage - it must be an interface to use it on processors.
	//================================================================================
	public interface ISimpleStage : IKCCStage<ISimpleStage>
	{
	}

	// 2. Example processor which implements the stage.
	//================================================================================
	public class ExampleProcessor1 : KCCProcessor, ISimpleStage
	{
		public void Execute(ISimpleStage stage, KCC kcc, KCCData data)
		{
			// Implementation of the custom stage.
		}
	}

	// 3. Example execution of the stage.
	//================================================================================
	public class ExampleProcessor2 : KCCProcessor, IPrepareData
	{
		public void Execute(PrepareData stage, KCC kcc, KCCData data)
		{
			// IPrepareData is default stage executed during KCC update.

			// Execution from within another stage as nested.
			kcc.ExecuteStage<ISimpleStage>();
		}
	}

	// 4. Another example execution of the stage.
	//================================================================================
	public class Player : NetworkBehaviour
	{
		public KCC KCC;

		public override void FixedUpdateNetwork()
		{
			// Execution from regular player script FUN.
			// It will be executed before/after KCC update (depends on execution order).
			// This way the pipeline can also be used for custom logic not related to movement.
			KCC.ExecuteStage<ISimpleStage>();
		}
	}
}
