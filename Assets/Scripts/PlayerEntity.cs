namespace VoidRogues.Players
{
    using UnityEngine;
    using Fusion;

    public interface IPlayer
    {
        string UserID { get; }
        string Nickname { get; }
        string UnityID { get; }
    }
    public struct FPlayerStatistics : INetworkStruct
    {
        public PlayerRef PlayerRef;
        public short ExtraLives;
        public short Kills;
        public short Deaths;
        public short Score;
        public TickTimer RespawnTimer;
        public byte Position;

        public byte KillsInRow;
        public TickTimer KillsInRowCooldown;
        public byte KillsWithoutDeath;

        public bool IsValid => PlayerRef.IsRealPlayer;
        public bool IsAlive { get { return _flags.IsBitSet(0); } set { _flags.SetBit(0, value); } }
        public bool IsEliminated { get { return _flags.IsBitSet(1); } set { _flags.SetBit(1, value); } }

        private byte _flags;
    }

    public sealed class PlayerEntity : NetworkBehaviour, IPlayer, IContextBehaviour
    {
        // PUBLIC MEMBERS

        public string UserID { get; private set; }
        public string UnityID { get; private set; }
        public string Nickname { get; private set; }
        public SceneContext Context { get; set; }
        public bool IsInitialized => _initCounter <= 0;

        [Networked]
        public PlayerCharacter ActivePlayerCharacter { get; set; }

        [Networked]
        public ref FPlayerStatistics Statistics => ref MakeRef<FPlayerStatistics>();

        [Networked]
        public NetworkBool IsReady { get; private set; }

        [Networked]
        public NetworkBool PlayerDataSet { get; private set; }

        [Networked]
        public NetworkBool HeroDataSet { get; private set; }

        // PRIVATE MEMBERS

        [Networked]
        private byte SyncToken { get; set; }
        [Networked]
        private NetworkString<_64> NetworkedUserID { get; set; }
        [Networked]
        private NetworkString<_32> NetworkedNickname { get; set; }

        private byte _syncToken;
        private PlayerRef _observedPlayer;
        //private Vector3 _lastViewPosition;
       // private Vector3 _lastViewDirection;
        //private bool _playerDataSent;
        //private bool _heroDataSent;
        private int _initCounter;
        private Coroutine _playerDataCoroutine;


        // PUBLIC METHODS

        public void DespawnCharacter()
        {
            if (Runner.IsServer == false)
                return;

            if (ActivePlayerCharacter != null && ActivePlayerCharacter.Object != null)
            {
                Runner.Despawn(ActivePlayerCharacter.Object);
                ActivePlayerCharacter = null;
            }
        }

        public void SetObservedPlayer(PlayerRef playerRef)
        {
            if (playerRef.IsRealPlayer == false)
            {
                playerRef = Object.InputAuthority;
            }

            if (playerRef == _observedPlayer)
                return;

            RPC_SetObservedPlayer(playerRef);
        }

        public void Refresh()
        {
            FPlayerStatistics statistics = Statistics;
            statistics.PlayerRef = Object.InputAuthority;
            Statistics = statistics;
        }

        public void OnReconnect(PlayerEntity newPlayer)
        {
            UserID = newPlayer.UserID;
            Nickname = newPlayer.Nickname;
            NetworkedUserID = newPlayer.NetworkedUserID;
            NetworkedNickname = newPlayer.NetworkedNickname;
            UnityID = newPlayer.UnityID;
        }

        // PlayerInterestManager INTERFACE

        public override void Spawned()
        {
            _syncToken = default;
            _observedPlayer = Object.InputAuthority;
            //_playerDataSent = false;
            _initCounter = 10;

            if (HasInputAuthority == true)
            {
                Context.LocalPlayerRef = Object.InputAuthority;
            }

            UpdateLocalState();

            Runner.SetIsSimulated(Object, true);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            DespawnCharacter();
        }

        public override void FixedUpdateNetwork()
        {
            UpdateLocalState();

            if (_syncToken != default && Runner.IsForward == true)
            {
                _initCounter = Mathf.Max(0, _initCounter - 1);
            }

            if (IsProxy == true)
                return;

            var observedPlayerCharacter = ActivePlayerCharacter;
            var observedPlayer = GetPlayerEntity(Runner, _observedPlayer);
            var observedPlayerRef = observedPlayerCharacter != null ? observedPlayerCharacter.Object.InputAuthority : Object.InputAuthority;

            if (HasInputAuthority)
            {
                Context.ObservedPlayerCharacter = observedPlayerCharacter;
                Context.ObservedPlayerRef = observedPlayerRef;
                Context.LocalPlayerRef = Object.InputAuthority;
                /*
                if (!_playerDataSent && Runner.IsForward == true && Context.PlayerData != null)
                {
                    FPlayerData playerData = new FPlayerData();

                    playerData.UserID = Context.PeerUserID;
                    playerData.Nickname = Context.PlayerData.Nickname;
                    playerData.UnityID = Context.PlayerData.UnityID != null ? Context.PlayerData.UnityID : string.Empty;

                    Debug.Log("Sending Player Data");

                    RPC_SendPlayerData(playerData);
                    _playerDataSent = true;
                }

                if (PlayerDataSet)
                {
                    if (_heroDataCoroutine == null)
                        _heroDataCoroutine = StartCoroutine(SendHeroDataUntilSet());
                }
                */
            }
        }      

        /*
        public override bool CanUpdatePlayerInterest(bool isUpdateRequested)
        {
            if (isUpdateRequested == true)
                return true;

            int interlacing = 50;
            int currentTick = Runner.Tick;

            if ((currentTick - LastUpdateTick) >= interlacing)
            {
                if ((currentTick % interlacing) == (Player.AsIndex % interlacing))
                    return true;
            }

            if (TryGetObservedPlayerView(out PlayerInterestView observedPlayerView) == true)
            {
                if (Vector3.SqrMagnitude(_lastViewPosition - observedPlayerView.CameraPosition) > 4.0f) // 2 meter distance change
                    return true;
                if (Vector3.Dot(_lastViewDirection, observedPlayerView.CameraDirection) < 0.99f) // Roughly 8 degrees change
                    return true;
            }

            return false;
        }

        protected override void AfterPlayerInterestUpdate(bool success, PlayerInterestView playerView)
        {
            if (success == true)
            {
                _lastViewPosition = playerView.CameraPosition;
                _lastViewDirection = playerView.CameraDirection;
            }
        }
        */
        // PRIVATE METHODS

        private void UpdateLocalState()
        {
            if (ActivePlayerCharacter)
            {
                _observedPlayer = Object.InputAuthority;

                /*
                if (_activeHero != null)
                {
                    InterestView = _activeHero.InterestView;
                }
                */
            }

            if (_syncToken != SyncToken)
            {
                _syncToken = SyncToken;

                UserID = NetworkedUserID.Value;
                Nickname = NetworkedNickname.Value;
            }

            /*
            if (ReferenceEquals(_platformAgent, _activeHero) == false && _activeHero != null)
            {
                if (_activeHero.Character.CharacterController.IsSpawned == true)
                {
                    BRPlatformProcessor platformProcessor = GetComponent<BRPlatformProcessor>();
                    _activeHero.Character.CharacterController.AddLocalProcessor(platformProcessor);
                    _platformAgent = _activeHero;
                }
            }
            */
        }

        [Rpc(RpcSources.StateAuthority | RpcSources.InputAuthority, RpcTargets.StateAuthority | RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
        private void RPC_SetObservedPlayer(PlayerRef player)
        {
            _observedPlayer = player;
        }

        public static PlayerEntity GetPlayerEntity(NetworkRunner runner, PlayerRef playerRef)
        {
            if (playerRef.IsRealPlayer == false)
                return default;

            if(runner == null)
            {
                Debug.LogError("Runner is null when trying to get player entity for player ref " + playerRef);
                return default;
            }

            var playerEntities = runner.GetAllBehaviours<PlayerEntity>();

            for (int i = 0, count = playerEntities.Count; i < count; ++i)
            {
                PlayerEntity player = playerEntities[i];
                if (player.Object.InputAuthority == playerRef)
                    return player;
            }

            return default;
        }

    }
}
