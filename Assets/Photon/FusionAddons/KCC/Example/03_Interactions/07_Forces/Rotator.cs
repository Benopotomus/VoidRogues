namespace Example.Forces
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Helper script to rotate objects based on simulation time.
	/// </summary>
	[DefaultExecutionOrder(-5000)]
	public sealed class Rotator : NetworkBehaviour
	{
		// PUBLIC MEMBERS

		public float Speed;
		public float Offset;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			Runner.SetIsSimulated(Object, true);
		}

		public override void FixedUpdateNetwork()
		{
			float rotation = Speed * Runner.SimulationTime + Offset;

			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

			Rigidbody rigidbody = GetComponentInChildren<Rigidbody>();
			rigidbody.position = rigidbody.transform.position;
		}

		public override void Render()
		{
			float rotation = Speed * (Runner.LocalRenderTime + Runner.DeltaTime) + Offset;

			transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);

			Rigidbody rigidbody = GetComponentInChildren<Rigidbody>();
			rigidbody.position = rigidbody.transform.position;
		}
	}
}
