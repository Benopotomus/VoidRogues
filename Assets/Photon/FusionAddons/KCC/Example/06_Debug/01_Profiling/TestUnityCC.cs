namespace Example.Profiling
{
	using UnityEngine;
	using UnityEngine.Profiling;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Simple script for testing collisions of Unity CC - moves forward or towards a target.
	/// </summary>
	public sealed class TestUnityCC : TestCC
	{
		// PRIVATE MEMBERS

		private Transform           _transform;
		private CharacterController _unityCC;

		// TestCC INTERFACE

		public override void ProcessFixedUpdate()
		{
			if (HasTarget == true)
			{
				Vector3 direction = (Target.position - transform.position).OnlyXZ();
				if (direction.sqrMagnitude < 1.0f)
				{
					ClearTarget();
				}

				direction.Normalize();

				transform.rotation = Quaternion.LookRotation(direction);
				Profiler.BeginSample("CharacterController.SimpleMove()");
				_unityCC.SimpleMove(direction * Speed);
				Profiler.EndSample();
			}
			else
			{
				Profiler.BeginSample("CharacterController.SimpleMove()");
				_unityCC.SimpleMove(_transform.forward.OnlyXZ() * Speed);
				Profiler.EndSample();
			}
		}

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Object.NetworkTypeId.IsSceneObject == false)
			{
				_unityCC.Move(_transform.position);
			}
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_transform = transform;
			_unityCC   = GetComponent<CharacterController>();
		}
	}
}
