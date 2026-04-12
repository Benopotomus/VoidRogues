using Fusion;
using UnityEngine;
using System.IO;
using VoidRogues.Players;

namespace VoidRogues
{
    public class PlayerSpawnManager : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _playerPrefab;
        public PlayerCharacter LocalPlayerCharacter { get; private set; }

        [SerializeField]
        private PlayerSpawnPoint[] _spawnPoints;

        [SerializeField] private Vector3 _fallbackSpawnPosition = new Vector3(1000, 0, 1000);

        public override void Spawned()
        {
            base.Spawned();
            _spawnPoints = FindObjectsByType<PlayerSpawnPoint>( FindObjectsSortMode.None);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear reference to avoid UI access after despawn
            LocalPlayerCharacter = null;
        }


        public void SpawnAllPlayerCharacters()
        {
            var playerEntities = Runner.GetAllBehaviours<PlayerEntity>();

            foreach (PlayerEntity playerEntity in playerEntities)
            {
                Debug.Log("Try to spawn player character");
                TrySpawnPlayerCharacter(playerEntity);
            }
        }

        public PlayerCharacter TrySpawnPlayerCharacter(PlayerEntity playerEntity)
        {
            if (playerEntity.ActivePlayerCharacter != null && playerEntity.ActivePlayerCharacter.Object != null)
                return playerEntity.ActivePlayerCharacter;

            (Vector3, Quaternion) spawnPosition = GetSpawnPosition();
            var playerRef = playerEntity.Object.InputAuthority;

            var spawnedPlayerCharacter = Runner.Spawn(_playerPrefab, spawnPosition.Item1, Quaternion.identity, playerRef, (runner, obj) =>
            {
                // Set owner reference immediately in the spawn callback,
                // before Spawned() is called on clients
                var character = obj.GetComponent<PlayerCharacter>();
                if (character != null)
                {
                    character.OwningPlayer = playerEntity;
                }
            });

            Runner.SetPlayerAlwaysInterested(playerRef, spawnedPlayerCharacter.Object, true);

            playerEntity.Statistics.IsAlive = true;
            playerEntity.Statistics.RespawnTimer = default;
            playerEntity.ActivePlayerCharacter = spawnedPlayerCharacter;

            return spawnedPlayerCharacter;
        }

        private string GetInstanceId()
        {
            // Extract project name from Application.dataPath (e.g., "C:/Projects/MyGame_clone_0/Assets")
            string path = Application.dataPath;
            string projectName = Path.GetFileName(Path.GetDirectoryName(path));
            return string.IsNullOrEmpty(projectName) ? "DefaultInstance" : projectName;
        }

        private (Vector3, Quaternion) GetSpawnPosition()
        {
            if (_spawnPoints != null && _spawnPoints.Length > 0)
            {
                var spawnPoint = _spawnPoints[0];
                return (spawnPoint.transform.position, spawnPoint.transform.rotation);
            }

            //Debug.Log("No spawn points available, using default position (0,0,0)");
            return (_fallbackSpawnPosition, Quaternion.identity);
        }
    }
}