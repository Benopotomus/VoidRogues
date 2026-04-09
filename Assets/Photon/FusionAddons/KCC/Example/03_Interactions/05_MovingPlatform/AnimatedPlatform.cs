namespace Example.MovingPlatform
{
	using System;
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Basic platform which moves the object by sampling AnimationClip. It must be executed first, before any player executes its movement.
	/// This script needs to be a KCC processor (deriving from NetworkKCCProcessor) to be correctly tracked by PlatformProcessor.
	/// It also implements IMapStatusProvider - providing status text about animation progress shown in UI.
	/// </summary>
	[RequireComponent(typeof(Rigidbody))]
    public sealed unsafe class AnimatedPlatform : NetworkKCCProcessor, IPlatform, IMapStatusProvider
    {
		// PRIVATE MEMBERS

		[SerializeField]
		private AnimationClip _animation;
		[SerializeField]
		private float _speed = 1.0f;
		[SerializeField]
		private bool _loop = true;

		[Networked]
		private int _baseTick { get; set; }

		private Transform _transform;
		private Rigidbody _rigidbody;

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (HasStateAuthority == true)
			{
				_baseTick = Runner.Tick;
			}
			else
			{
				SetTransform(Runner.Tick);
			}

			// By default FixedUpdateNetwork() is not executed on proxies.
			// We register callbacks to ensure the platform is predicted everywhere.
			// The platform is updated first because PlatformManager has lower execution order.
			PlatformManager manager = Runner.GetSingleton<PlatformManager>();
			manager.OnFixedUpdateNetwork += OnFixedUpdateNetwork;
			manager.OnRender += OnRender;
		}

		public override void Despawned(NetworkRunner runner, bool hasState)
		{
			PlatformManager manager = runner.GetSingleton<PlatformManager>();
			manager.OnFixedUpdateNetwork -= OnFixedUpdateNetwork;
			manager.OnRender -= OnRender;
		}

		// IMapStatusProvider INTERFACE

		bool IMapStatusProvider.IsActive(PlayerRef player)
		{
			return true;
		}

		string IMapStatusProvider.GetStatus(PlayerRef player)
		{
			float tick = Runner.Tick + Runner.LocalAlpha;
			float time = (tick - _baseTick) * Runner.DeltaTime * _speed;

			return $"{name} - {Mathf.RoundToInt(time / _animation.length * 100.0f)}%";
		}

		// MonoBehaviour INTERFACE

		private void Awake()
		{
			_transform = transform;
			_rigidbody = GetComponent<Rigidbody>();

			if (_rigidbody == null)
				throw new NullReferenceException($"GameObject {name} has missing Rigidbody component!");

			_rigidbody.isKinematic   = true;
			_rigidbody.useGravity    = false;
			_rigidbody.interpolation = RigidbodyInterpolation.None;
			_rigidbody.constraints   = RigidbodyConstraints.FreezeAll;
		}

		// PRIVATE METHODS

		private void OnFixedUpdateNetwork()
		{
			SetTransform(Runner.Tick);
		}

		private void OnRender()
		{
			SetTransform(Runner.Tick + Runner.LocalAlpha);
		}

		private void SetTransform(float tick)
		{
			float time = (tick - _baseTick) * Runner.DeltaTime * _speed;
			time = _loop == true ? time % _animation.length : Mathf.Min(time, _animation.length);

			_animation.SampleAnimation(gameObject, time);

			_rigidbody.position = transform.position;
			_rigidbody.rotation = transform.rotation;
		}
	}
}
