
/// <summary>
/// Holds scene specific references and common runtime data.
/// </summary>
/// 
namespace LichLord
{
    using Fusion;
    using LichLord.Missions;
    using LichLord.NonPlayerCharacters;
    using LichLord.Projectiles;
    using LichLord.Props;
    using LichLord.UI;
    using LichLord.World;
    using UnityEngine;

    [System.Serializable]
    public class SceneContext
    {
        public bool IsVisible;
        public bool HasInput;
        public string PeerUserID;

        public NetworkRunner Runner;
        public NetworkGame NetworkGame;
        public SpawnManager SpawnManager;
        public ProjectileManager ProjectileManager;
        public PropManager PropManager;
        public WorldSaveLoadManager WorldSaveLoadManager;
        public PlayerSaveLoadManager PlayerSaveLoadManager;
        public NonPlayerCharacterManager NonPlayerCharacterManager;
        public WorldManager WorldManager;
        public ChunkManager ChunkManager;
        public SceneCamera Camera;
        public SceneUI UI;
        public LairManager LairManager;
        public InvasionManager InvasionManager;
        public ContainerManager ContainerManager;
        public DialogManager DialogManager;
        public MissionManager MissionManager;
        public DebugConsole DebugConsole;
        public VisualEffectManager VFXManager;

        [HideInInspector]
        public PlayerRef LocalPlayerRef;
        [HideInInspector]
        public PlayerCharacter LocalPlayerCharacter;

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
        public PlayerRef ObservedPlayerRef;
        [HideInInspector]
        public HeroEntity ObservedHeroEntity;
        [HideInInspector]
        public GlobalSettings Settings;
        [HideInInspector]
        public RuntimeSettings RuntimeSettings;
        [HideInInspector]
        public GameplayMode GameplayMode;
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
