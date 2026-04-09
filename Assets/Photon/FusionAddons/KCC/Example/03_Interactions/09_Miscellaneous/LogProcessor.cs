namespace Example.Miscellaneous
{
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - logging callbacks and stage execution to console using KCC.Log() method.
	/// </summary>
	public sealed class LogProcessor : KCCProcessor, IBeginMove, IPrepareData, ISetGravity, ISetDynamicVelocity, ISetKinematicDirection, ISetKinematicTangent, ISetKinematicSpeed, ISetKinematicVelocity, IAfterMoveStep, IEndMove
	{
		// KCCProcessor INTERFACE

		public override float GetPriority(KCC kcc) => float.MaxValue;

		public override void OnEnter      (KCC kcc, KCCData data) { kcc.Log(nameof(OnEnter));       }
		public override void OnExit       (KCC kcc, KCCData data) { kcc.Log(nameof(OnExit));        }
		public override void OnStay       (KCC kcc, KCCData data) { kcc.Log(nameof(OnStay));        }
		public override void OnInterpolate(KCC kcc, KCCData data) { kcc.Log(nameof(OnInterpolate)); }

		// Default stage INTERFACES

		public void Execute(BeginMove     stage, KCC kcc, KCCData data) { kcc.Log(nameof(IBeginMove));     }
		public void Execute(PrepareData   stage, KCC kcc, KCCData data) { kcc.Log(nameof(IPrepareData));   }
		public void Execute(AfterMoveStep stage, KCC kcc, KCCData data) { kcc.Log(nameof(IAfterMoveStep)); }
		public void Execute(EndMove       stage, KCC kcc, KCCData data) { kcc.Log(nameof(IEndMove));       }

		// EnvironmentKCCProcessor stage INTERFACES

		public void Execute(ISetGravity            stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetGravity));            }
		public void Execute(ISetDynamicVelocity    stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetDynamicVelocity));    }
		public void Execute(ISetKinematicDirection stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetKinematicDirection)); }
		public void Execute(ISetKinematicTangent   stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetKinematicTangent));   }
		public void Execute(ISetKinematicSpeed     stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetKinematicSpeed));     }
		public void Execute(ISetKinematicVelocity  stage, KCC kcc, KCCData data) { kcc.Log(nameof(ISetKinematicVelocity));  }
	}
}
