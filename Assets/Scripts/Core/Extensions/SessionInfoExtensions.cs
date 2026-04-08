using Fusion;

namespace VoidRogues
{
    public static class SessionInfoExtensions
    {
        public static string GetDisplayName(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.DISPLAY_NAME_KEY, out SessionProperty name) == true)
                return name;

            return info.Name;
        }

        public static bool HasLevel(this SessionInfo info)
        {
            return info.Properties.ContainsKey(Networking.LEVEL_ID);
        }

        public static int GetLevelID(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.LEVEL_ID, out SessionProperty levelIndex) == false)
                return default;

            return (int)levelIndex;
        }

        public static int GetLevelSeed(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.LEVEL_SEED, out SessionProperty levelIndex) == false)
                return default;

            return (int)levelIndex;
        }

        public static GameMode GetGameMode(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.MODE_KEY, out SessionProperty mode) == false)
                return default;

            return (GameMode)(int)mode;
        }

        public static int GetGameSessionID(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.GAME_SESSION_ID, out SessionProperty sessionID) == false)
                return default;

            return (int)sessionID;
        }

        public static GameSessionDetails GetGameSessionDetails(this SessionInfo info)
        {
            if (info.Properties.TryGetValue(Networking.GAME_SESSION_ID, out SessionProperty sessionID) == false)
                return default;

            return Global.Settings.GameSessions.GetGameSession((int)sessionID);
        }
    }
}
