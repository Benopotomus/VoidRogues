namespace Example.ScriptableProcessor
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	[CreateAssetMenu(menuName = "KCC/Example Processor (ScriptableObject)")]
	public sealed class ExampleProcessor : ScriptableKCCProcessor, IPrepareData
	{
		[SerializeField]
		private float _gravityMultiplier;

		// ScriptableKCCProcessor INTERFACE

		public override float GetPriority(KCC kcc)
		{
			// Min priority = executed last.
			return float.MinValue;
		}

		// IPrepareData INTERFACE

		public void Execute(PrepareData stage, KCC kcc, KCCData data)
		{
			data.Gravity *= _gravityMultiplier;
		}
	}
}
