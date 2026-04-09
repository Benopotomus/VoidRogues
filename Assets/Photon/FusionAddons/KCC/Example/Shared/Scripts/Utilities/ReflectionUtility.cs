namespace Example
{
	using System.Reflection;
	using Fusion;

	public static class ReflectionUtility
	{
		// PRIVATE MEMBERS

		private static readonly FieldInfo _simulationFieldInfo = typeof(NetworkRunner).GetField("_simulation", BindingFlags.Instance | BindingFlags.NonPublic);

		// PUBLIC METHODS

		public static void GetInterpolationData(NetworkRunner runner, out int fromTick, out int toTick, out float alpha)
		{
			Simulation simulation = (Simulation)_simulationFieldInfo.GetValue(runner);

			if (runner.IsServer == true)
			{
				fromTick = simulation.TickPrevious;
				toTick   = simulation.Tick;
				alpha    = simulation.LocalAlpha;
			}
			else
			{
				fromTick = simulation.RemoteTickPrevious;
				toTick   = simulation.RemoteTick;
				alpha    = simulation.RemoteAlpha;
			}
		}
	}
}
