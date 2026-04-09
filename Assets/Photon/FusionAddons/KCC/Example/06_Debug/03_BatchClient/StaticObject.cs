namespace Example.BatchClient
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Script for simulation of internal Fusion overhead of static objects.
	/// Used for interest management stress testing.
	/// </summary>
	public sealed class StaticObject : NetworkBehaviour
	{
		[Networked]
		private int State01 { get; set; }
		[Networked]
		private Vector3 State02 { get; set; }
	}
}
