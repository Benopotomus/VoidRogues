namespace Example.Profiling
{
	using UnityEngine;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Simple script for testing collisions of Fusion KCC - moves forward or towards a target.
	/// </summary>
	public sealed class TestKCC : TestCC
	{
		// PUBLIC MEMBERS

		public KCC KCC => _kcc;

		// PRIVATE MEMBERS

		private KCC _kcc;

		// TestCC INTERFACE

		public override void ProcessFixedUpdate()
		{
			if (HasTarget == true)
			{
				Vector3 direction = (Target.position - _kcc.Data.TargetPosition).OnlyXZ();
				if (direction.sqrMagnitude < 1.0f)
				{
					ClearTarget();
				}

				direction.Normalize();

				_kcc.SetLookRotation(Quaternion.LookRotation(direction));
				_kcc.SetInputDirection(direction);
			}
			else
			{
				_kcc.SetInputDirection(_kcc.Data.TransformDirection);
			}

			_kcc.FixedData.KinematicVelocity = _kcc.FixedData.InputDirection * Speed;

			_kcc.ManualFixedUpdate();
		}

		public override void ProcessRenderUpdate()
		{
			_kcc.ManualRenderUpdate();
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			base.Spawned();

			_kcc.SetManualUpdate(true);
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_kcc = gameObject.GetComponent<KCC>();
			_kcc.SetManualUpdate(true);
		}
	}
}
