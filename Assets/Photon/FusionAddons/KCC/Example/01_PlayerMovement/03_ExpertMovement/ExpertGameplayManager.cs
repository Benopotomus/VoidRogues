namespace Example.ExpertMovement
{
	using System.Collections.Generic;
	using UnityEngine;
	using Fusion;

	/// <summary>
	/// Main entry point for gameplay logic and spawning players.
	/// There exists only ONE instance spawned in the scene.
	/// </summary>
	public sealed class ExpertGameplayManager : NetworkBehaviour
	{
		// PUBLIC MEMBERS

		public NetworkObject[] PlayerPrefabs;
		public NetworkObject[] AbilityPrefabs;

		// PRIVATE MEMBERS

		[HideInInspector]
		public int SelectedPlayerPrefab = 0;

		// List of all player configs.
		private Dictionary<PlayerRef, ExpertPlayerConfig> _playerConfigs = new Dictionary<PlayerRef, ExpertPlayerConfig>();

		// NetworkBehaviour INTERFACE

		public override void Spawned()
		{
			if (Runner.GameMode == GameMode.Shared)
			{
				// In Shared mode every player spawn the player object on their own.
				// The SpawnPlayer method fetches player data from player config so we need to set it first.
				SetPlayerConfig(Runner.LocalPlayer, SelectedPlayerPrefab);
				SpawnPlayer(Runner.LocalPlayer);
			}
			else
			{
				// With Client-Server topology the Server spawn player objects.
				// The server waits until it receives player config from the client via RPC.
				SendPlayerConfigRPC(Runner.LocalPlayer, SelectedPlayerPrefab);
			}
		}

		public override void FixedUpdateNetwork()
		{
			if (Runner.IsServer == true)
			{
				// With Client-Server topology only the Server spawn player objects.
				// PlayerManager is a special helper class which iterates over list of active players (NetworkRunner.ActivePlayers) and call spawn/despawn callbacks on demand.
				PlayerManager<ExpertPlayer>.UpdatePlayerConnections(Runner, SpawnPlayer, DespawnPlayer);
			}
		}

		// PRIVATE METHODS

		private void SpawnPlayer(PlayerRef playerRef)
		{
			// Spawn player object only to players who sent their player config.
			if (_playerConfigs.TryGetValue(playerRef, out ExpertPlayerConfig playerConfig) == false)
				return;

			// Get all spawnpoints in the scene.
			SpawnPoint[] spawnPoints = Runner.SimulationUnityScene.GetComponents<SpawnPoint>(false);

			// Select random spawnpoint.
			Transform spawnPoint = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)].transform;

			// Spawn the player object with correct input authority.
			NetworkObject player = Runner.Spawn(PlayerPrefabs[playerConfig.SelectedPlayerPrefab], spawnPoint.position, spawnPoint.rotation, playerRef);

			ExpertPlayer expertPlayer = player.GetComponent<ExpertPlayer>();
			if (expertPlayer == null)
				throw new System.Exception($"Missing {nameof(ExpertPlayer)} component on prefab {PlayerPrefabs[playerConfig.SelectedPlayerPrefab].name}!");

			// Set the spawned instance as player object so we can easily get it from other locations using Runner.GetPlayerObject(playerRef).
			// This is optional, but it is a good practice as there is usually 1 main object spawned for each player.
			Runner.SetPlayerObject(playerRef, player);

			// Every player should be always interested to his player object to prevent accidentally getting out of Area of Interest.
			// This is valid only if the Interest Management is enabled in Network Project Config.
			Runner.SetPlayerAlwaysInterested(playerRef, player, true);

			// Add abilities - they are spawned as child objects of the player.
			// IKCCProcessor abilities are not automatically registered to KCC. This has to be done manually by using KCC.AddModifier() or KCC.TryAddModifier().
			ExpertPlayerAbilities playerAbilities = player.GetComponent<ExpertPlayerAbilities>();
			if (playerAbilities != null)
			{
				for (int i = 0; i < AbilityPrefabs.Length; ++i)
				{
					playerAbilities.AddAbility(AbilityPrefabs[i]);
				}
			}
		}

		private void DespawnPlayer(PlayerRef playerRef, ExpertPlayer player)
		{
			// We simply despawn the player object. Ideally there is no additional cleanup needed.
			// Abilities should be correctly cleaned up in ExpertPlayerAbilities.Despawned().
			Runner.Despawn(player.Object);
		}

		private void SetPlayerConfig(PlayerRef playerRef, int selectedPlayerPrefab)
		{
			ExpertPlayerConfig playerConfig = new ExpertPlayerConfig();
			playerConfig.PlayerRef = playerRef;
			playerConfig.SelectedPlayerPrefab = SelectedPlayerPrefab;

			_playerConfigs[playerRef] = playerConfig;
		}

		[Rpc(RpcSources.All, RpcTargets.StateAuthority)]
		private void SendPlayerConfigRPC(PlayerRef playerRef, int selectedPlayerPrefab)
		{
			SetPlayerConfig(playerRef, selectedPlayerPrefab);
		}
	}
}
