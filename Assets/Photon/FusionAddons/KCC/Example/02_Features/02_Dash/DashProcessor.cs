namespace Example.Dash
{
	using UnityEngine;
	using Fusion;
	using Fusion.Addons.KCC;
	using Example.Portal;
	using Example.Teleport;

	/// <summary>
	/// Example processor - moves the player in forward direction over time.
	/// This processor also reacts on teleport events and recalculates direction if the KCC rotation changes.
	/// This processor has networked state and must be spawned by Fusion.
	/// </summary>
	public sealed class DashProcessor : NetworkKCCProcessor, IBeginMove, ISetKinematicDirection, ISetKinematicSpeed, ITeleportListener, IPortalListener
	{
		// PUBLIC MEMBERS

		public Vector3 Direction => _direction;

		// PRIVATE MEMBERS

		[SerializeField]
		private float _speed = 50.0f;
		[SerializeField]
		private float _distance = 10.0f;
		[SerializeField]
		private bool  _stopOnTeleport = false;

		[Networked]
		private Vector3 _direction         { get; set; }
		[Networked]
		private float   _remainingTime     { get; set; }
		[Networked]
		private float   _remainingDistance { get; set; }

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			// This object is needed on state & input authority only => Object Interest is set to Explicit Players.
			// Input authority should always be a player who owns the dash ability.
			Object.SetPlayerAlwaysInterested(Object.InputAuthority, true);
		}

		// NetworkKCCProcessor INTERFACE

		// Priority equals to dash speed. The faster you move, the higher priority.
		public override float GetPriority(KCC kcc) => _speed;

		public override void OnEnter(KCC kcc, KCCData data)
		{
			if (kcc.IsInFixedUpdate == false)
				return;

			if (data.InputDirection.IsAlmostZero() == false)
			{
				// If the KCC has an input direction, we'll start dashing in this direction.
				_direction = data.InputDirection.normalized;
			}
			else
			{
				// Otherwise we'll start dashing in look direction.
				_direction = Quaternion.Euler(data.LookPitch, data.LookYaw, 0.0f) * Vector3.forward;
			}

			_remainingTime     = _distance / _speed;
			_remainingDistance = _distance;
		}

		public override void OnStay(KCC kcc, KCCData data)
		{
			if (kcc.IsInFixedUpdate == false)
				return;

			// Check for max travel time.
			// This is needed when moving against a wall (remaining distance will never reach zero).
			_remainingTime -= data.DeltaTime;
			if (_remainingTime <= 0.0f)
			{
				// Dash has ended, cleanup.
				kcc.RemoveModifier(this);
				return;
			}

			if (data.HasTeleported == false)
			{
				// Check for max travel distance.
				// This is needed when moving with higher speed.
				_remainingDistance -= Vector3.Distance(data.BasePosition, data.TargetPosition);
				if (_remainingDistance <= 0.0f)
				{
					// Dash has ended, cleanup.
					kcc.RemoveModifier(this);
					return;
				}
			}
		}

		// IBeginMove INTERFACE

		public void Execute(BeginMove stage, KCC kcc, KCCData data)
		{
			// In case of very fast movement the CCD should be enabled and the prediction correction disabled to prevent passing through geometry and minimize visual glitches.
			kcc.EnforceFeature(EKCCFeature.CCD);
			kcc.SuppressFeature(EKCCFeature.PredictionCorrection);
		}

		// ISetKinematicDirection INTERFACE

		// Explicit implementation of stage priority, this will be preferred over processor Priority property.
		// We want to override direction regardless of the default processor priority, this processor must be executed first.
		float IKCCStage<ISetKinematicDirection>.GetPriority(KCC kcc) => float.MaxValue;

		public void Execute(ISetKinematicDirection stage, KCC kcc, KCCData data)
		{
			// Override kinematic direction by the value stored on activation.
			data.KinematicDirection = _direction;

			// Suppress all other processors.
			kcc.SuppressProcessors<IKCCProcessor>();
		}

		// ISetKinematicSpeed INTERFACE

		public void Execute(ISetKinematicSpeed stage, KCC kcc, KCCData data)
		{
			// Dashing with higher speed is allowed.
			if (data.KinematicSpeed < _speed)
			{
				data.KinematicSpeed = _speed;
			}

			// Suppress all other processors with same stage and lower priority.
			// Suppressing processors from same category (IAbilityProcessor) is not needed as it is covered by supressing ISetKinematicSpeed.
			kcc.SuppressProcessors<ISetKinematicSpeed>();
		}

		// ITeleportListener INTERFACE

		void ITeleportListener.OnTeleport(KCC kcc, KCCData data)
		{
			HandleTeleport(kcc, data);
		}

		// IPortalListener INTERFACE

		void IPortalListener.OnTeleport(KCC kcc, KCCData data)
		{
			HandleTeleport(kcc, data);
		}

		// PRIVATE METHODS

		private void HandleTeleport(KCC kcc, KCCData data)
		{
			// The KCC just dashed into a Teleport or a Portal, this may need special handling.
			// ITeleportListener/IPortalListener is user defined interface for processors intercommunication.
			// This way a processor can react to events triggered by other processors.

			if (_stopOnTeleport == true)
			{
				_remainingTime = 0.0f;
			}
			else
			{
				// Update dash direction based on new look direction after teleport.
				_direction = Quaternion.Euler(data.LookPitch, data.LookYaw, 0.0f) * Vector3.forward;
			}
		}
	}
}
