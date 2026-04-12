namespace VoidRogues
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using VoidRogues.Players;
    using VoidRogues.NonPlayerCharacters;
    using Fusion;
    using Pathfinding;
    using System;

    public struct KillData : INetworkStruct
    {
        public PlayerRef KillerRef;
        public PlayerRef VictimRef;
        private byte _flags;
    }

    public struct RespawnRequest
    {
        public PlayerRef PlayerRef;
        public TickTimer Timer;
    }

    public enum EGameplayModeType
    {
        None,
        Lobby,
        Deathmatch,
        BattleRoyale,
        Elimination,
        BossFight,
        Extraction,
        Survival,
    }

    public enum EGameplayModePhase
    {
        None,
        Loading,
        PreMatch,
        Active,
        PostMatch,
        Finished,
    }

    public class GameplayMode : ContextBehaviour
    {
        public string GameplayName;
        public int MaxPlayers;
        public short ScorePerKill;
        public short ScorePerDeath;
        public short ScorePerSuicide;
        public float RespawnTime;
        public float TimeLimit;
        public float BackfillTimeLimit;

        [Header("NPC Spawning")]
        [SerializeField] private float _npcSpawnInterval = 10f;
        [SerializeField] private float _npcSpawnRadius = 20f;
        [SerializeField] private NonPlayerCharacterDefinition _npcDefinition;
        [SerializeField] private ENPCSpawnType _npcSpawnType = ENPCSpawnType.Invader;
        [SerializeField] private ETeamID _npcTeamID;
        [SerializeField] private EAttitude _npcAttitude = EAttitude.Hostile;

        // PUBLIC EVENTS
        public Action<PlayerRef> OnPlayerJoinedGame;
        public Action<string> OnPlayerLeftGame;
        public Action<KillData> OnAgentDeath;
        public Action<PlayerRef> OnPlayerEliminated;

        // PRIVATE MEMBERS
        private DefaultPlayerComparer _playerComparer = new DefaultPlayerComparer();
        private float _backfillTimerS;
        private List<PlayerCharacter> _allPlayers = new List<PlayerCharacter>(); // Reused list for performance

        [Networked]
        private TickTimer _npcSpawnTimer { get; set; }

        // PUBLIC METHODS
        public void Activate()
        {
            if (HasStateAuthority)
            {
                SetLevelData();
                Context.PlayerSpawnManager.SpawnAllPlayerCharacters();
            }
            OnActivate();
        }

        public void SetLevelData()
        {
            if (Runner.SessionInfo == null)
                return;
        }

        public void ChangeSpectatorTarget(bool next)
        {
            var observedPlayerRef = Context.ObservedPlayerRef;
            int playerIndex = 0;
            int maxPlayerIndex = 1000;

            while (playerIndex < maxPlayerIndex)
            {
                ++playerIndex;
                if (observedPlayerRef.AsIndex > maxPlayerIndex)
                {
                    observedPlayerRef = PlayerRef.None;
                }
                else if (observedPlayerRef.AsIndex < PlayerRef.None.AsIndex)
                {
                    observedPlayerRef = PlayerRef.FromIndex(maxPlayerIndex);
                }

                observedPlayerRef = PlayerRef.FromIndex(observedPlayerRef.AsIndex + (next ? 1 : -1));

                PlayerEntity observedPlayer = PlayerEntity.GetPlayerEntity(Runner, observedPlayerRef);
                if (observedPlayer == null || observedPlayer.Statistics.IsEliminated)
                    continue;

                break;
            }

            var localPlayer = PlayerEntity.GetPlayerEntity(Runner, Context.LocalPlayerRef);
            if (localPlayer != null)
                localPlayer.SetObservedPlayer(observedPlayerRef);
        }

        public virtual void OnPlayerJoined(PlayerEntity playerEntity)
        {
            Debug.Log("Player Joined");
            PreparePlayerStatistics(ref playerEntity.Statistics);
            AssignTeamToPlayer(playerEntity);
            Context.PlayerSpawnManager.TrySpawnPlayerCharacter(playerEntity);

            RecalculateScorePositions();
            RPC_PlayerJoinedGame(playerEntity.Object.InputAuthority);
        }

        public virtual void OnPlayerLeft(PlayerEntity player)
        {
            if (!HasStateAuthority)
                return;

            player.DespawnCharacter();
            RecalculateScorePositions();

            string nickname = player.Nickname;
            if (string.IsNullOrEmpty(nickname))
                nickname = "Unknown";

            RPC_PlayerLeftGame(player.Object.InputAuthority, nickname);
            CheckWinCondition();
        }

        // NETWORKBEHAVIOUR INTERFACE
        public override void Spawned()
        {
            Context.GameplayMode = this;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
        }

        public override void FixedUpdateNetwork()
        {
            if (!HasStateAuthority)
                return;

            if (_npcDefinition != null && _npcSpawnInterval > 0f && _npcSpawnTimer.ExpiredOrNotRunning(Runner))
            {
                TrySpawnNPCOnNavmesh();
                _npcSpawnTimer = TickTimer.CreateFromSeconds(Runner, _npcSpawnInterval);
            }
        }

        // GAMEPLAYMODE INTERFACE
        protected virtual void OnActivate()
        {
            if (HasStateAuthority && _npcDefinition != null && _npcSpawnInterval > 0f)
            {
                _npcSpawnTimer = TickTimer.CreateFromSeconds(Runner, _npcSpawnInterval);
            }
        }

        protected virtual void PreparePlayerStatistics(ref FPlayerStatistics playerStatistics) { }
        protected virtual void AssignTeamToPlayer(PlayerEntity player) { }

        protected virtual void SortPlayers(List<FPlayerStatistics> allStatistics)
        {
            allStatistics.Sort(_playerComparer);
        }

        protected virtual float GetRespawnTime(FPlayerStatistics playerStatistics)
        {
            return RespawnTime;
        }

        private void OnLevelGenerated()
        {
            Debug.Log("Level Loaded, Setting up Mode Elements");
        }

        // UPDATED NPC SPAWNING - 10 to 30 units away from ALL players
        private void TrySpawnNPCOnNavmesh()
        {
            var manager = Context.NonPlayerCharacterManager;
            if (manager == null)
                return;

            if (!GetValidNPCSpawnPoint(out Vector3 spawnPos))
                return;

            manager.SpawnNPC(spawnPos, _npcDefinition, _npcSpawnType, _npcTeamID, _npcAttitude);
        }

        private bool GetValidNPCSpawnPoint(out Vector3 result)
        {
            result = Vector3.zero;

            if (AstarPath.active == null)
                return false;

            // Get all alive PlayerCharacter positions
            GetAllAlivePlayerPositions(_allPlayers);

            if (_allPlayers.Count == 0)
            {
                // Fallback if no players are alive
                return GetPointOnNavmesh(transform.position, _npcSpawnRadius, out result);
            }

            const int MAX_ATTEMPTS = 60;
            const float MIN_DISTANCE = 10f;
            const float MAX_DISTANCE = 30f;

            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                // Pick a random player character as reference
                int randomIndex = UnityEngine.Random.Range(0, _allPlayers.Count);
                Vector3 referencePos = _allPlayers[randomIndex].transform.position;

                // Generate random direction and distance between 10-30 units
                float distance = UnityEngine.Random.Range(MIN_DISTANCE, MAX_DISTANCE);
                Vector3 randomDir = UnityEngine.Random.onUnitSphere;
                randomDir.y = 0f;
                randomDir.Normalize();

                Vector3 candidatePos = referencePos + randomDir * distance;

                // Snap to navmesh (with small search radius)
                if (GetPointOnNavmesh(candidatePos, 6f, out Vector3 navmeshPos))
                {
                    // Final check: must be at least 10m from EVERY player
                    if (IsFarEnoughFromAllPlayers(navmeshPos, _allPlayers, MIN_DISTANCE))
                    {
                        result = navmeshPos;
                        return true;
                    }
                }
            }

            // Fallback if we couldn't find a good spot after many attempts
            Debug.LogWarning($"[{GameplayName}] Could not find valid NPC spawn 10-30m from players after {MAX_ATTEMPTS} attempts. Using fallback.");
            return GetPointOnNavmesh(transform.position, _npcSpawnRadius, out result);
        }

        // Helper: Populates the list with all alive PlayerCharacters
        private void GetAllAlivePlayerPositions(List<PlayerCharacter> playerList)
        {
            playerList.Clear();

            // Using the method you specified
            Runner.GetAllBehaviours<PlayerCharacter>(playerList);

            // Remove dead / eliminated players
            for (int i = playerList.Count - 1; i >= 0; i--)
            {
                var pc = playerList[i];
                if (pc == null)
                {
                    playerList.RemoveAt(i);
                    continue;
                }

                var playerEntity = pc.GetComponentInParent<PlayerEntity>(); // Adjust if needed
                if (playerEntity != null && playerEntity.Statistics.IsEliminated)
                {
                    playerList.RemoveAt(i);
                }
            }
        }

        // Helper: Check distance to all players
        private bool IsFarEnoughFromAllPlayers(Vector3 point, List<PlayerCharacter> players, float minDistance)
        {
            foreach (var pc in players)
            {
                if (pc == null) continue;

                if (Vector3.Distance(point, pc.transform.position) < minDistance)
                    return false;
            }
            return true;
        }

        // Original helper (kept for fallback)
        private bool GetPointOnNavmesh(Vector3 center, float radius, out Vector3 result)
        {
            result = center;

            if (AstarPath.active == null)
                return false;

            Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * radius;
            randomPoint.y = center.y;

            var constraint = NNConstraint.Default;
            constraint.constrainWalkability = true;
            constraint.walkable = true;

            var info = AstarPath.active.GetNearest(randomPoint, constraint);

            if (info.node == null)
                return false;

            result = info.position;
            return true;
        }

        protected void CheckWinCondition()
        {
        }

        protected void FinishGameplay()
        {
            if (Runner.IsServer == false)
                return;

            Runner.SessionInfo.IsOpen = false;

            if (Application.isBatchMode)
            {
                StartCoroutine(ShutdownCoroutine());
            }
        }

        protected void SetSpectatorTargetToBestPlayer(PlayerEntity spectatorPlayer)
        {
            var bestPlayer = PlayerRef.None;
            int bestPosition = int.MaxValue;

            foreach (var player in Context.NetworkGame.ActivePlayers)
            {
                if (player == null || player.Statistics.IsEliminated)
                    continue;

                int position = player.Statistics.Position > 0 ? player.Statistics.Position : 1000;
                if (position < bestPosition)
                {
                    bestPlayer = player.Statistics.PlayerRef;
                    bestPosition = position;
                }
            }

            spectatorPlayer.SetObservedPlayer(bestPlayer);
        }

        private void Respawn(PlayerRef playerRef)
        {
            var player = PlayerEntity.GetPlayerEntity(Runner, playerRef);
            if (player == null)
                return;

            player.DespawnCharacter();
            Context.PlayerSpawnManager.TrySpawnPlayerCharacter(player);
        }

        private void RecalculateScorePositions()
        {
            var allStatistics = ListPool.Get<FPlayerStatistics>(byte.MaxValue);

            foreach (var player in Context.NetworkGame.ActivePlayers)
            {
                if (player == null)
                    continue;

                var statistics = player.Statistics;
                if (statistics.IsValid == false)
                    continue;

                allStatistics.Add(statistics);
            }

            SortPlayers(allStatistics);
            ListPool.Return(allStatistics);
        }

        private IEnumerator ShutdownCoroutine()
        {
            yield return DWD.Utility.StaticTimer.Seconds(20.0f);
            Debug.LogWarning("Shutting down...");
            Application.Quit();
        }

        // RPCs
        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_PlayerJoinedGame(PlayerRef playerRef)
        {
            OnPlayerJoinedGame?.Invoke(playerRef);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
        private void RPC_PlayerLeftGame(PlayerRef playerRef, string nickname)
        {
            OnPlayerLeftGame?.Invoke(nickname);
        }

        // HELPERS
        private class DefaultPlayerComparer : IComparer<FPlayerStatistics>
        {
            public int Compare(FPlayerStatistics x, FPlayerStatistics y)
            {
                return y.Score.CompareTo(x.Score);
            }
        }
    }

    public enum eLevelPhase : byte
    {
        None = 0,
        Ferry = 1,
        First = 2,
        Second = 3,
        Post = 4,
    }
}