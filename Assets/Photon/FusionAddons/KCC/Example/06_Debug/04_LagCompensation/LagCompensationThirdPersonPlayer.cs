namespace Example.LagCompensation
{
	using UnityEngine;
	using Example.ExpertMovement;
	using Fusion;
	using Fusion.Addons.KCC;

	/// <summary>
	/// Player script for testing calculation of render position based on Runner.LocalAlpha passed through input from a client.
	/// This calculation is useful for render accurate shooting in combination with lag compensation.
	/// </summary>
	public sealed class LagCompensationThirdPersonPlayer : ThirdPersonExpertPlayer
	{
		const int MAX_RECORDS = 100;

		[SerializeField]
		private KCCTransformSampler _cameraHandleSampler;
		[SerializeField]
		private float _raycastLength = 50.0f;

		[Networked]
		private int _handleCount { get; set; }
		[Networked][Capacity(MAX_RECORDS)]
		private NetworkArray<FireRecord> _handleRecords => default;

		[Networked]
		private int _accurateCount { get; set; }
		[Networked][Capacity(MAX_RECORDS)]
		private NetworkArray<FireRecord> _accurateRecords => default;

		private int          _fixedCount;
		private FireRecord[] _fixedRecords = new FireRecord[MAX_RECORDS];

		private int          _renderKCCCount;
		private FireRecord[] _renderKCCRecords = new FireRecord[MAX_RECORDS];

		private int          _renderCameraCount;
		private FireRecord[] _renderCameraRecords = new FireRecord[MAX_RECORDS];

		protected override void ProcessInputAfterFixedMovement()
		{
			base.ProcessInputAfterFixedMovement();

			_cameraHandleSampler.Sample(KCC);
		}

		protected override void ProcessInputSortedByLocalAlpha()
		{
			base.ProcessInputSortedByLocalAlpha();

			if (Runner.IsForward == true)
			{
				Debug.DrawLine(KCC.Data.TargetPosition, KCC.Data.TargetPosition + Vector3.up * 2.0f, Color.black * 0.5f, 300.0f);

				CameraHandle.GetPositionAndRotation(out Vector3 cameraHandlePosition, out Quaternion cameraHandleRotation);
				Debug.DrawLine(cameraHandlePosition - Vector3.up * 0.2f, cameraHandlePosition + Vector3.up * 0.5f, Color.gray * 0.5f, 300.0f);
			}

			if (Input.WasActivated(EExpertInputAction.LMB) == true && Runner.IsForward == true)
			{
				if (HasStateAuthority == true)
				{
					CameraHandle.GetPositionAndRotation(out Vector3 cameraHandlePosition, out Quaternion cameraHandleRotation);
					FireHandle(cameraHandlePosition, cameraHandleRotation);
				}

				if (_cameraHandleSampler.ResolveRenderPositionAndRotation(KCC, Input.FixedInput.LocalAlpha, out Vector3 renderCameraPosition, out Quaternion renderCameraRotation) == true)
				{
					FireAccurate(renderCameraPosition, renderCameraRotation);
				}
				else
				{
					KCC.LogError("Missing data for lag calculation of render position and rotation.");
				}
			}
		}

		protected override void ProcessInputAfterRenderMovement()
		{
			_cameraHandleSampler.Sample(KCC);

			base.ProcessInputAfterRenderMovement();

			if (Input.WasActivated(EExpertInputAction.LMB) == true)
			{
				CameraHandle.GetPositionAndRotation(out Vector3 cameraHandlePosition, out Quaternion cameraHandleRotation);
				FireRender(cameraHandlePosition, cameraHandleRotation, _renderCameraRecords, ref _renderCameraCount);

				if (_cameraHandleSampler.ResolveRenderPositionAndRotation(KCC, Input.RenderInput.LocalAlpha, out Vector3 renderCameraPosition, out Quaternion renderCameraRotation) == true)
				{
					FireRender(renderCameraPosition, renderCameraRotation, _renderKCCRecords, ref _renderKCCCount);
				}
				else
				{
					KCC.LogError("Missing data for lag calculation of render position and rotation.");
				}
			}
		}

		private void FireHandle(Vector3 position, Quaternion rotation)
		{
			FireRecord fireRecord = new FireRecord();
			fireRecord.IsValid   = true;
			fireRecord.PlayerHit = false;
			fireRecord.Position  = position;
			fireRecord.Rotation  = rotation;

			KCCShapeCastInfo shapeCastInfo = new KCCShapeCastInfo();
			if (KCC.RayCast(shapeCastInfo, position, rotation * Vector3.forward, _raycastLength, QueryTriggerInteraction.Ignore) == true)
			{
				if (shapeCastInfo.AllHits[0].RaycastHit.rigidbody != null)
				{
					fireRecord.PlayerHit = true;
				}
			}

			if (HasStateAuthority == true)
			{
				_handleRecords.Set(_handleCount % MAX_RECORDS, fireRecord);
				++_handleCount;
			}
		}

		private void FireAccurate(Vector3 position, Quaternion rotation)
		{
			FireRecord fireRecord = new FireRecord();
			fireRecord.IsValid   = true;
			fireRecord.PlayerHit = false;
			fireRecord.Position  = position;
			fireRecord.Rotation  = rotation;

			KCCShapeCastInfo shapeCastInfo = new KCCShapeCastInfo();
			if (KCC.RayCast(shapeCastInfo, position, rotation * Vector3.forward, _raycastLength, QueryTriggerInteraction.Ignore) == true)
			{
				if (shapeCastInfo.AllHits[0].RaycastHit.rigidbody != null)
				{
					fireRecord.PlayerHit = true;
				}
			}

			if (HasInputAuthority == true)
			{
				_fixedRecords[_fixedCount % MAX_RECORDS] = fireRecord;
				++_fixedCount;
			}

			if (HasStateAuthority == true)
			{
				_accurateRecords.Set(_accurateCount % MAX_RECORDS, fireRecord);
				++_accurateCount;
			}
		}

		private void FireRender(Vector3 position, Quaternion rotation, FireRecord[] records, ref int recordCount)
		{
			FireRecord fireRecord = new FireRecord();
			fireRecord.IsValid   = true;
			fireRecord.PlayerHit = false;
			fireRecord.Position  = position;
			fireRecord.Rotation  = rotation;

			KCCShapeCastInfo shapeCastInfo = new KCCShapeCastInfo();
			if (KCC.RayCast(shapeCastInfo, position, rotation * Vector3.forward, _raycastLength, QueryTriggerInteraction.Ignore) == true)
			{
				if (shapeCastInfo.AllHits[0].RaycastHit.rigidbody != null)
				{
					fireRecord.PlayerHit = true;
				}
			}

			records[recordCount % MAX_RECORDS] = fireRecord;
			++recordCount;
		}

		private void LateUpdate()
		{
			if (HasInputAuthority == false)
				return;

			int fixedHits    = default;
			int handleHits   = default;
			int accurateHits = default;

			for (int i = 0; i < MAX_RECORDS; ++i)
			{
				FireRecord fireRecord = _renderCameraRecords[i];
				if (fireRecord.IsValid == true)
				{
					DrawLines(fireRecord.Position, fireRecord.Rotation, fireRecord.PlayerHit, Color.cyan, Vector3.back);
				}
			}

			for (int i = 0; i < MAX_RECORDS; ++i)
			{
				FireRecord fireRecord = _renderKCCRecords[i];
				if (fireRecord.IsValid == true)
				{
					DrawLines(fireRecord.Position, fireRecord.Rotation, fireRecord.PlayerHit, Color.blue, Vector3.left);
				}
			}

			for (int i = 0; i < MAX_RECORDS; ++i)
			{
				FireRecord fireRecord = _fixedRecords[i];
				if (fireRecord.IsValid == true)
				{
					if (fireRecord.PlayerHit == true)
					{
						++fixedHits;
					}

					DrawLines(fireRecord.Position, fireRecord.Rotation, fireRecord.PlayerHit, Color.yellow, Vector3.down);
				}
			}

			for (int i = 0; i < MAX_RECORDS; ++i)
			{
				FireRecord fireRecord = _handleRecords[i];
				if (fireRecord.IsValid == true)
				{
					if (fireRecord.PlayerHit == true)
					{
						++handleHits;
					}

					DrawLines(fireRecord.Position, fireRecord.Rotation, fireRecord.PlayerHit, Color.white, Vector3.down);
				}
			}

			for (int i = 0; i < MAX_RECORDS; ++i)
			{
				FireRecord fireRecord = _accurateRecords[i];
				if (fireRecord.IsValid == true)
				{
					if (fireRecord.PlayerHit == true)
					{
						++accurateHits;
					}

					DrawLines(fireRecord.Position, fireRecord.Rotation, fireRecord.PlayerHit, Color.magenta, Vector3.right);
				}
			}

			if ((int)Time.unscaledTime != (int)(Time.unscaledTime - Time.unscaledDeltaTime))
			{
				KCC.Log($"Client (R) VS. Server (R): {fixedHits} / {accurateHits}     Client (R) VS. Server (T): {fixedHits} / {handleHits}");
			}
		}

		private void DrawLines(Vector3 position, Quaternion rotation, bool playerHit, Color color, Vector3 direciton)
		{
			Debug.DrawLine(position, position + rotation * Vector3.forward * _raycastLength, color);
			Debug.DrawLine(position, position + rotation * direciton * 0.5f, color);
			Debug.DrawLine(position, position + rotation * direciton * 0.25f, playerHit == true ? Color.green : Color.red);
		}

		private struct FireRecord : INetworkStruct
		{
			public NetworkBool IsValid;
			public NetworkBool PlayerHit;
			public Vector3     Position;
			public Quaternion  Rotation;
		}
	}
}
