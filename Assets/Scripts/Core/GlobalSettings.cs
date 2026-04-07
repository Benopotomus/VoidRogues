using UnityEngine;
using System;
using Fusion;

namespace LichLord
{
    [Serializable]
    [CreateAssetMenu(fileName = "GlobalSettings", menuName = "LichLord/Settings/Global Settings")]
    public class GlobalSettings : ScriptableObject
    {
        public NetworkRunner RunnerPrefab;

        public bool SimulateMobileInput;

        /*
        [Header("Settings")]

        public HeroSettings Heroes;
        public ScenesSettings Scenes;
        public GameSessionSettings GameSessions;
        public NetworkSettings Network;
        */

        [Space]
        public OptionsData DefaultOptions;

        [Space]
        public StandaloneConfiguration DebugConnection;

        [Space]
        public TestLoadout TestLoadout;


    }
}
