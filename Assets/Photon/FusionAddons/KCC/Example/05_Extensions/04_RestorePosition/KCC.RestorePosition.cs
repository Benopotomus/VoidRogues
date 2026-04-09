// This functionality is disabled by default to save performance.
/*
namespace Fusion.Addons.KCC
{
	using UnityEngine;

	/// <summary>
	/// By default KCC, Transform and Rigidbody state are reset from network buffer in IAfterClientPredictionReset (received new state from the server), followed by fixed and render updates.
	/// Next Unity frame before simulating forward tick (no resimulation), Transform and Rigidbody have state from last render update.
	/// This might result in an inconsistent simulation and visual glitches when more KCCs and physics objects collide.
	/// To fix this, following code restores state before simulation.
	/// This is not a problem for example on server running with -batchmode and disabled Render() calls.
	/// Use this only if you experience problems described above and it clearly helps you. Otherwise take it just as another example how to extend KCC.
	/// </summary>
	public partial class KCC : IBeforeAllTicks
	{
		void IBeforeAllTicks.BeforeAllTicks(bool resimulation, int tickCount)
		{
			// Skip resimulation, the state is already restored from KCC.AfterClientPredictionReset().
			if (resimulation == true)
				return;

			// Restore state only if it's enabled in settings.
			// One way to increase precision only for players. This can be disabled on NPCs.
			if (_settings.RestorePosition == false)
				return;

			// Restore state only if a render update was executed previous frame.
			// Otherwise we continue with state from previous fixed tick or the state is already restored from KCC.AfterClientPredictionReset().
			int previousFrame = Time.frameCount - 1;
			if (previousFrame != _lastRenderFrame)
				return;

			// Synchronize only position from fixed data, without anti-jitter feature.
			// This updates both Transform and Rigidbody components.
			SynchronizeTransform(_fixedData, true, false, false, false);
		}
	}
}
*/
