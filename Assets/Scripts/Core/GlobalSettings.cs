using UnityEngine;
using System;
using Fusion;

namespace VoidRogues
{
    [Serializable]
    [CreateAssetMenu(fileName = "GlobalSettings", menuName = "VoidRogues/Settings/Global Settings")]
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
