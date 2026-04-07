using Fusion;

namespace VoidRogues
{
    public struct FSessionRequest
    {
        public string UserID;
        public GameMode GameMode;
        public string DisplayName;
        public string SessionName;
        public string ScenePath;
        //public EGameplayType GameplayType;
        public int LevelSequenceID;
        //public eLevelPhase LevelPhase;
        public int GameSessionID;
        public int LevelSeed;
        public int MaxPlayers;
        public string CustomLobby;
        public string IPAddress;
        public ushort Port;
    }
}