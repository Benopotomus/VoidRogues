namespace Example.BatchClient
{
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Main entry point for gameplay logic, spawning players and processing batch mode parameters.
	/// There exists only ONE instance spawned in the scene.
	/// </summary>
	public sealed class BatchGameplayManager : NetworkBehaviour
	{
		// PUBLIC MEMBERS

		public FusionBootstrap Bootstrap;
		public BatchPlayer     PlayerPrefab;
		public StaticObject    StaticObjectPrefab;
		public Waypoints       Waypoints;
		public int             WaypointsCount = 100;
		public float           WaypointsRadius = 500.0f;
		public float           SpawnRadius = 500.0f;
		public int             Players = 0;
		public int             StaticObjects = 0;

		// PRIVATE MEMBERS

		private static bool       _hasStarted;
		private static Collider[] _overlapColliders = new Collider[1];

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (HasStateAuthority == true)
			{
				SpawnWaypoints();
				SpawnServerPlayers();
				SpawnStaticObjects();
			}

			if (Runner.GameMode == GameMode.Shared)
			{
				// In Shared mode every player spawn the player object on their own.
				SpawnPlayer(Runner.LocalPlayer);
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.IsServer == true)
			{
				// With Client-Server topology only the Server spawn player objects.
				// PlayerManager is a special helper class which iterates over list of active players (NetworkRunner.ActivePlayers) and call spawn/despawn callbacks on demand.
				PlayerManager<BatchPlayer>.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);
			}
		}

		// MonoBehaviour INTERFACE

		private void Start()
		{
			if (ApplicationUtility.GetCommandLineArgument("-waypoints", out int waypoints) == true)
			{
				WaypointsCount = Mathf.Clamp(waypoints, 3, 1000);
			}

			if (ApplicationUtility.GetCommandLineArgument("-radius", out int radius) == true)
			{
				radius = Mathf.Clamp(radius, 10, 2000);

				WaypointsRadius = radius;
				SpawnRadius     = radius;
			}

			if (ApplicationUtility.GetCommandLineArgument("-aoiCellSize", out int aoiCellSize) == true)
			{
				Simulation.AreaOfInterest.CELL_SIZE = aoiCellSize;
			}

			if (ApplicationUtility.GetCommandLineArgument("-players", out int players) == true)
			{
				Players = Mathf.Max(0, players);
			}

			if (ApplicationUtility.GetCommandLineArgument("-staticObjects", out int staticObjects) == true)
			{
				StaticObjects = Mathf.Max(0, staticObjects);
			}

			if (ApplicationUtility.GetCommandLineArgument("-room", out string room) == true)
			{
				Bootstrap.DefaultRoomName = room;
			}

			if (ApplicationUtility.GetCommandLineArgument("-port", out int port) == true)
			{
				Bootstrap.ServerPort = (ushort)port;
			}

			if (_hasStarted == true)
				return;

			_hasStarted = true;

			if (ApplicationUtility.HasCommandLineArgument("-server") == true)
			{
				Bootstrap.StartServer();
			}
			else if (Players > 1)
			{
				Bootstrap.StartMultipleClients(Players);
			}
			else if (Application.isBatchMode == true)
			{
				Bootstrap.StartAutoClient();
			}
		}

		// PRIVATE METHODS

		private void SpawnWaypoints()
		{
			Transform waypointsGroup = new GameObject("Group").transform;
			waypointsGroup.SetParent(Waypoints.transform);

			for (int i = 0; i < WaypointsCount; ++i)
			{
				Transform waypoint = new GameObject($"Waypoint ({i})").transform;
				waypoint.SetParent(waypointsGroup);
				waypoint.position = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f) * Vector3.forward * Random.Range(WaypointsRadius * 0.5f, WaypointsRadius);
			}
		}

		private void SpawnServerPlayers()
		{
			for (int i = 0; i < Players; ++i)
			{
				BatchPlayer player = Runner.Spawn(PlayerPrefab, GetPlayerSpawnPosition(), Quaternion.identity);
				player.SetServerControlled();
			}
		}

		private void SpawnStaticObjects()
		{
			for (int i = 0; i < StaticObjects; ++i)
			{
				Runner.Spawn(StaticObjectPrefab, GetPlayerSpawnPosition(), Quaternion.identity);
			}
		}

		private void SpawnPlayer(PlayerRef playerRef)
		{
			// Spawn the player object with correct input authority.
			BatchPlayer player = Runner.Spawn(PlayerPrefab, GetPlayerSpawnPosition(), Quaternion.identity, playerRef);

			// Set the spawned instance as player object so we can easily get it from other locations using Runner.GetPlayerObject(playerRef).
			// This is optional, but it is a good practice as there is usually 1 main object spawned for each player.
			Runner.SetPlayerObject(playerRef, player.Object);

			// Every player should be always interested to his player object to prevent accidentally getting out of Area of Interest.
			// This is valid only if the Interest Management is enabled in Network Project Config.
			Runner.SetPlayerAlwaysInterested(playerRef, player.Object, true);
		}

		private void DespawnPlayer(PlayerRef playerRef, BatchPlayer player)
		{
			if (player.IsServerControlled == true)
				return;

			// We simply despawn the player object. No other cleanup is needed here.
			Runner.Despawn(player.Object);
		}

		private Vector3 GetPlayerSpawnPosition()
		{
			float   spawnOffset   = 1.0f;
			Vector3 spawnPosition = default;

			for (int i = 0; i < 1000; ++i)
			{
				spawnPosition = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f) * Vector3.forward * Random.Range(SpawnRadius * 0.5f, SpawnRadius);
				spawnPosition.y += spawnOffset;

				if (Runner.GetPhysicsScene().OverlapSphere(spawnPosition, 0.25f, _overlapColliders, -1, QueryTriggerInteraction.Ignore) == 0)
					break;
			}

			return spawnPosition;
		}
	}
}
