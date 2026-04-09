namespace Example.MovingPlatform
{
	using System;
	using UnityEngine;
	using Fusion;

	[DefaultExecutionOrder(-2000)]
	public sealed class PlatformManager : SimulationBehaviour, IDespawned
	{
		public event Action OnFixedUpdateNetwork;
		public event Action OnRender;

		public override void FixedUpdateNetwork()
		{
			OnFixedUpdateNetwork?.Invoke();
		}

		public override void Render()
		{
			OnRender?.Invoke();
		}

		void IDespawned.Despawned(NetworkRunner runner, bool hasState)
		{
			OnFixedUpdateNetwork = default;
			OnRender             = default;
		}
	}
}
