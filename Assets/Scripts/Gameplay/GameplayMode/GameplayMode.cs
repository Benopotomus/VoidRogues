
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

        // public Announcement[] Announcements;

        // PUBLIC MEMBERS

        public Action<PlayerRef> OnPlayerJoinedGame;
        public Action<string> OnPlayerLeftGame;
        public Action<KillData> OnAgentDeath;
        public Action<PlayerRef> OnPlayerEliminated;

        // PROTECTED MEMBERS
        
        // PRIVATE MEMBERS

        private DefaultPlayerComparer _playerComparer = new DefaultPlayerComparer();
        private float _backfillTimerS;

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

                observedPlayerRef = PlayerRef.FromIndex(observedPlayerRef.AsIndex + (next == true ? 1 : -1));

                PlayerEntity observedPlayer = PlayerEntity.GetPlayerEntity(Runner, observedPlayerRef);
                if (observedPlayer == null)
                    continue;

                if (observedPlayer.Statistics.IsEliminated == true)
                    continue;

                break;
            }

            var localPlayer = PlayerEntity.GetPlayerEntity(Runner, Context.LocalPlayerRef);
            localPlayer.SetObservedPlayer(observedPlayerRef);
        }

        public virtual void OnPlayerJoined(PlayerEntity playerEntity)
        {
            Debug.Log("Player Joined");

            PreparePlayerStatistics(ref playerEntity.Statistics);
            AssignTeamToPlayer(playerEntity);

            Context.PlayerSpawnManager.TrySpawnPlayerCharacter(playerEntity);
            // Check the behavior and see if we late spawn.

            RecalculateScorePositions();

            //Context.Backfill.PlayerJoined(player);

            RPC_PlayerJoinedGame(playerEntity.Object.InputAuthority);
        }

        public virtual void OnPlayerLeft(PlayerEntity player)
        {
            if (!HasStateAuthority)
                return;

            player.DespawnCharacter();

            RecalculateScorePositions();

            //Context.Backfill.PlayerLeft(player);

            string nickname = player.Nickname;
            if (nickname.HasValue() == false)
            {
                nickname = "Unknown";
            }

            RPC_PlayerLeftGame(player.Object.InputAuthority, nickname);

            CheckWinCondition();
        }

        // NetworkBehaviour INTERFACE

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

            if (_npcDefinition != null && _npcSpawnInterval > 0f && _npcSpawnTimer.Expired(Runner))
            {
                TrySpawnNPCOnNavmesh();
                _npcSpawnTimer = TickTimer.CreateFromSeconds(Runner, _npcSpawnInterval);
            }
        }

        // GameplayMode INTERFACE

        protected virtual void OnActivate()
        {
            if (HasStateAuthority && _npcDefinition != null && _npcSpawnInterval > 0f)
            {
                _npcSpawnTimer = TickTimer.CreateFromSeconds(Runner, _npcSpawnInterval);
            }
        }

        protected virtual void PreparePlayerStatistics(ref FPlayerStatistics playerStatistics) {}

        protected virtual void AssignTeamToPlayer(PlayerEntity player) {}

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

        private void TrySpawnNPCOnNavmesh()
        {
            var manager = Context.NonPlayerCharacterManager;
            if (manager == null)
                return;

            Vector3 origin = transform.position;

            Vector3 spawnPos;
            if (!GetPointOnNavmesh(origin, _npcSpawnRadius, out spawnPos))
                return;

            manager.SpawnNPC(spawnPos, _npcDefinition, _npcSpawnType, _npcTeamID, _npcAttitude);
        }

        private bool GetPointOnNavmesh(Vector3 center, float radius, out Vector3 result)
        {
            result = center;

            if (AstarPath.active == null)
                return false;

            Vector3 randomPoint = center + UnityEngine.Random.insideUnitSphere * radius;
            randomPoint.y = center.y;

            var info = AstarPath.active.GetNearest(randomPoint, NNConstraint.Walkable);
            if (info.node == null)
                return false;

            result = info.position;
            return true;
        }

        protected void CheckWinCondition()
        {
        }

        // PROTECTED METHODS

        protected void FinishGameplay()
        {
            if (Runner.IsServer == false)
                return;

            Runner.SessionInfo.IsOpen = false;
            //Context.Backfill.BackfillEnabled = false;

            if (Application.isBatchMode == true)
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
                if (player == null)
                    continue;

                var statistics = player.Statistics;
                if (statistics.IsEliminated == true)
                    continue;

                int position = statistics.Position > 0 ? statistics.Position : 1000;

                if (position < bestPosition)
                {
                    bestPlayer = statistics.PlayerRef;
                    bestPosition = position;
                }
            }

            spectatorPlayer.SetObservedPlayer(bestPlayer);
        }

        // PRIVATE METHODS

        protected virtual void FixedUpdateNetwork_Active()
        {

            _backfillTimerS += Time.deltaTime;
            if (_backfillTimerS > BackfillTimeLimit)
            {
                //Context.Backfill.BackfillEnabled = false;
            }

            /*
            if (_endTimer.Expired(Runner) == true)
            {
                FinishGameplay();
            }
            */
        }

        protected virtual void FixedUpdateNetwork_Finished()
        {
        }

        private void Respawn(PlayerRef playerRef)
        {
            var player = PlayerEntity.GetPlayerEntity(Runner, playerRef);
            if (player == null)
                return; // Player is not present anymore

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
