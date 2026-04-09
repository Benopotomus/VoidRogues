
/// <summary>
/// Holds scene specific references and common runtime data.
/// </summary>
/// 
namespace VoidRogues
{
    using Fusion;
    using Pathfinding;
    using UnityEngine;

    [System.Serializable]
    public class SceneContext
    {
        public bool IsVisible;
        public bool HasInput;
        public string PeerUserID;

        public NetworkRunner Runner;
        public NetworkGame NetworkGame;
        public PlayerSpawnManager PlayerSpawnManager;
        public SceneCamera Camera;
        public NonPlayerCharacterManager NonPlayerCharacterManager;
        public AstarPath Pathfinding;
        /*
        public ProjectileManager ProjectileManager;
        public PropManager PropManager;
        public WorldSaveLoadManager WorldSaveLoadManager;
        public PlayerSaveLoadManager PlayerSaveLoadManager;
        public WorldManager WorldManager;
        public ChunkManager ChunkManager;
        public SceneUI UI;
        public LairManager LairManager;
        public InvasionManager InvasionManager;
        public ContainerManager ContainerManager;
        public DialogManager DialogManager;
        public MissionManager MissionManager;
        public DebugConsole DebugConsole;
        public VisualEffectManager VFXManager;
        */

        [HideInInspector]
        public PlayerRef LocalPlayerRef;
        [HideInInspector]
        public PlayerCharacter LocalPlayerCharacter;
        [HideInInspector]
        public PlayerRef ObservedPlayerRef;
        [HideInInspector]
        public PlayerCharacter ObservedPlayerCharacter;
        [HideInInspector]
        public GameplayMode GameplayMode;

        // General
        /*
        public ObjectCache ObjectCache;
        //public GeneralInput GeneralInput;

        public Matchmaking Matchmaking;

        public SceneInput Input;

        public ActorEventManager ActorEventManager;

        public ImpactManager ImpactManager;
        public GameplayEffectManager GameplayEffectManager;
        public LevelManager LevelManager;
        public PropManager PropManager;
        public CreatureManager CreatureManager;
        public AstarPath Pathfinding;
        public HitManager HitManager;


        // Gameplay

        [HideInInspector]

        [HideInInspector]
        public PlayerRef LocalPlayerRef;

        [HideInInspector]
        public GlobalSettings Settings;
        [HideInInspector]
        public RuntimeSettings RuntimeSettings;

        [HideInInspector]
        public NetworkLobby Lobby;
                */
        public bool IsGameplayActive()
        {
            return true;
            /*
            if (GameplayMode == null)
                return false;

            if (GameplayMode.GamePhaseStateMachine.PhaseName != eGameplayModePhase.Active)
                return false;

            return true;
            */
        }    
    }

    public enum ESceneContextCategory
    { 
        None,
        DialogManager,
        MissionManager,
        NonPlayerCharacterManager,
        InvasionManager,
        LocalPlayerCharacter,
    }

}
