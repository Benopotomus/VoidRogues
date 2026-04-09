// Game sessions are a combination of the GameplayModeType and the Map as well
// as any additional stuff to add later

// They are selected as a single block to create a lobby.

namespace VoidRogues
{
    using UnityEngine;
    using System;
    using System.Collections.Generic;

    [Serializable]
    [CreateAssetMenu(fileName = "GameSessionSettings", menuName = "Settings/GameSession Settings")]

    public sealed class GameSessionSettings : ScriptableObject
    {
        // PUBLIC MEMBERS

        public GameSessionDetails[] GameSessions => _gameSessions;
        // PRIVATE MEMBERS

        [SerializeField]
        private GameSessionDetails[] _gameSessions;


        public GameSessionDetails GetGameSession(int id)
        { 
            return _gameSessions[id];
        }

        public int GetGameSessionID(GameSessionDetails gameSession)
        {
            for (int i = 0; i < _gameSessions.Length; i++)
            {
                if(gameSession == _gameSessions[i])
                    return i;
            }

            return 0;
        }

        public List<GameSessionDetails> GetVisibleSessions()
        { 
        
            List<GameSessionDetails> visibleSessions = new List<GameSessionDetails>();

            GameSessionDetails[] allSessions = GameSessions;

            for (int i = 0; i < allSessions.Length; i++)
            {
                if (allSessions[i].ShowInSessionSelection)
                    visibleSessions.Add(allSessions[i]);
            }

            return visibleSessions;
        }

        // Gets the next visible session index based on a passed index
        public int GetNextVisibleSessionIndex(int currentIndex)
        {
            // Get the visible sessions
            List<int> visibleSessionIndices = GetVisibleSessionIndices();
            if (visibleSessionIndices.Count == 0) return -1; // No visible sessions available

            // Find the position of the current index in the list of visible session indices
            int currentVisiblePosition = visibleSessionIndices.IndexOf(currentIndex);

            if (currentVisiblePosition == -1) return -1; // Passed index is not in the visible sessions

            // Get the next visible session index, wrapping around
            int nextVisiblePosition = (currentVisiblePosition + 1) % visibleSessionIndices.Count;

            return visibleSessionIndices[nextVisiblePosition];
        }

        // Gets the next visible session index based on a passed index
        public int GetPreviousVisibleSessionIndex(int currentIndex)
        {
            // Get the visible sessions
            List<int> visibleSessionIndices = GetVisibleSessionIndices();
            if (visibleSessionIndices.Count == 0) return -1; // No visible sessions available

            // Find the position of the current index in the list of visible session indices
            int currentVisiblePosition = visibleSessionIndices.IndexOf(currentIndex);

            if (currentVisiblePosition == -1) return -1; // Passed index is not in the visible sessions

            // Get the previous visible session index, wrapping around
            int previousVisiblePosition = (currentVisiblePosition - 1 + visibleSessionIndices.Count) % visibleSessionIndices.Count;

            return visibleSessionIndices[previousVisiblePosition];
        }

        // Helper: Gets the indices of visible sessions
        private List<int> GetVisibleSessionIndices()
        {
            List<int> visibleIndices = new List<int>();

            for (int i = 0; i < GameSessions.Length; i++)
            {
                if (GameSessions[i].ShowInSessionSelection)
                {
                    visibleIndices.Add(i);
                }
            }

            return visibleIndices;
        }

    }

    // ===========================================================================

    [Serializable]
    public sealed class GameSessionDetails
    {
        // PUBLIC MEMBERS

        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Image => _image;

        public bool ShowInSessionSelection => _showInSessionSelection;
        
        // PRIVATE MEMBERS

        [SerializeField]
        private string _displayName;
        [SerializeField, TextArea(3, 6)]
        private string _description;
        [SerializeField]
        private Sprite _image;
        [SerializeField]
        private bool _showInSessionSelection = true;
    }
}
