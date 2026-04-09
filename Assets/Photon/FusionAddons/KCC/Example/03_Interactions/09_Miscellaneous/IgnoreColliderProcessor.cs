namespace Example.Miscellaneous
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - adding specific collider to internal KCC ignore list.
	/// In case of blocking collider the KCC will be able to go through and will not interact with it.
	/// The ignored collider requires NetworkObject component.
	/// </summary>
	public sealed class IgnoreColliderProcessor : KCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Collider _collider;

		// KCCProcessor INTERFACE

		public override void OnEnter(KCC kcc, KCCData data)
		{
			kcc.SetIgnoreCollider(_collider, true);
		}

		public override void OnExit(KCC kcc, KCCData data)
		{
			kcc.SetIgnoreCollider(_collider, false);
		}
	}
}
