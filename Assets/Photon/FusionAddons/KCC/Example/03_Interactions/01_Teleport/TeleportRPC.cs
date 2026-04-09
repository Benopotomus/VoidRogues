namespace Example.Teleport
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Example processor - teleport to specific position and optionally setting look rotation using KCC.TeleportRPC().
	/// This implementation is here only for example purposes and should not be used that way in production.
	/// The TeleportRPC is useful for debugging purposes and makes easier player placement which is driven by client.
	/// Requires KCCSettings.AllowClientTeleports to be enabled.
	/// Typical scenario:
	/// 1) Server sets KCCSettings.AllowClientTeleports to true.
	/// 2) Client clicks on a map and calls TeleportRPC() with given position (custom Player placement phase).
	/// 3) Server sets KCCSettings.AllowClientTeleports to false after some time.
	/// 4) Gameplay starts. Players are already on their selected positions.
	/// </summary>
	public sealed class TeleportRPC : KCCProcessor
	{
		// PRIVATE MEMBERS

		[SerializeField]
		private Transform[] _targets;

		// KCCProcessor INTERFACE

		public override void OnEnter(KCC kcc, KCCData data)
		{
			// Teleport is executed only from fixed update.
			if (kcc.IsInFixedUpdate == false)
				return;

			if (_targets.Length == 0)
			{
				Debug.LogError($"Missing target on {nameof(TeleportRPC)} {name}", gameObject);
				return;
			}

			if (kcc.Settings.AllowClientTeleports == false)
			{
				Debug.LogError($"{nameof(KCCSettings)}.{nameof(KCCSettings.AllowClientTeleports)} must be enabled to use {nameof(KCC)}.{nameof(TeleportRPC)}().", gameObject);
				return;
			}

			// Call RPC on client only.
			if (kcc.HasInputAuthority == true)
			{
				Transform target = _targets[Random.Range(0, _targets.Length)];
				kcc.TeleportRPC(target.position, kcc.FixedData.LookPitch, kcc.FixedData.LookYaw);
			}
		}
	}
}
