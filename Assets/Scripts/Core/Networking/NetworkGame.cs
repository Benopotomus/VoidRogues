#define ENABLE_LOGS

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Fusion.Sockets;
using UnityEditor;

namespace VoidRogues
{

    public sealed class NetworkGame : ContextBehaviour
    {
        // PUBLIC MEMBERS
        private List<PlayerCharacter> _activePlayers = new List<PlayerCharacter>();
        public List<PlayerCharacter> ActivePlayers => _activePlayers;

        public event Action GameLoaded;

        public void Initialize()
        {
        }

        public void OnPlayerSpawned(PlayerCharacter pc)
        {
            if (_activePlayers.Contains(pc))
                return;

            _activePlayers.Add(pc);
        }

        public void OnPlayerDespawned(PlayerCharacter pc)
        {
            _activePlayers.Remove(pc);
        }

        public IEnumerator Activate(int levelIndex, int levelGeneratorSeed)
        {
            /*
            while (Context.GameplayMode == null)
                yield return null;

            while (Context.LevelManager == null)
                yield return null;

            Debug.Log("Activating Game Mode");
            Context.GameplayMode.Activate();

            while (Context.LevelManager.LoadedLevel == null)
                yield return null;

            */

            Cursor.lockState = CursorLockMode.Locked;

            GameLoaded?.Invoke();
            yield return null;
        }

        public PlayerCharacter GetPlayerCharacter(PlayerRef playerRef)
        {
            if (!playerRef.IsRealPlayer)
                return null;

            foreach (PlayerCharacter player in ActivePlayers)
            {
                if (player.Object.InputAuthority == playerRef)
                    return player;
            }

            return null;
        }

        public int GetFreePlayerIndex()
        {
            const int MaxPlayers = 16;
            HashSet<int> usedIndices = new HashSet<int>();

            for (int i = 0; i < _activePlayers.Count; i++)
            {
                usedIndices.Add(_activePlayers[i].PlayerIndex);
            }

            for (int i = 0; i < MaxPlayers; i++)
            {
                if (!usedIndices.Contains(i))
                    return i;
            }

            return -1;
        }

        public PlayerCharacter GetPlayerByIndex(int targetIndex)
        {
            for (int i = 0; i < _activePlayers.Count; i++)
            {
                if (_activePlayers[i].SpawnComplete &&
                    _activePlayers[i].PlayerIndex == targetIndex)
                    return _activePlayers[i];
            }

            return null;
        }
    }
}