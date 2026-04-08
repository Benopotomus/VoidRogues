using Fusion;
using UnityEngine;
using System.IO;

namespace VoidRogues
{
    public class PlayerSpawnManager : ContextBehaviour
    {
        [SerializeField] private PlayerCharacter _playerPrefab;
        public PlayerCharacter LocalPlayerCharacter { get; private set; }

        [SerializeField]
        private PlayerSpawnPoint[] _spawnPoints;

        [SerializeField] private Vector3 _fallbackSpawnPosition = new Vector3(1000, 0, 1000);

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            // Clear reference to avoid UI access after despawn
            LocalPlayerCharacter = null;
        }

        public void SpawnLocalPlayer(PlayerRef playerRef)
        {
            if (LocalPlayerCharacter != null)
                return;

            if (!Runner.IsPlayerValid(Runner.LocalPlayer))
            {
                Debug.LogWarning("LocalPlayer is invalid, cannot spawn player!");
                return;
            }


                CreateAndSpawnPlayer(playerRef);

        }

        private void CreateAndSpawnPlayer(PlayerRef playerRef)
        {
            (Vector3, Quaternion) spawnPosition = GetSpawnPosition();

            LocalPlayerCharacter = Runner.Spawn(_playerPrefab, spawnPosition.Item1, spawnPosition.Item2, inputAuthority: playerRef);

            Debug.Log($"Create local player at {spawnPosition} with Nickname {LocalPlayer.Nickname}");
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