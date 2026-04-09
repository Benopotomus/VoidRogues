using UnityEngine;

namespace VoidRogues
{
    /// <summary>
    /// ScriptableObject that defines a single NPC type's statistics and assets.
    ///
    /// Create via: Assets → Create → VoidRogues → NPC Definition
    /// </summary>
    [CreateAssetMenu(menuName = "VoidRogues/NPC Definition", fileName = "NewNPC")]
    public class NPCDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string NPCName = "Unnamed NPC";

        [Header("Behaviour")]
        [Tooltip("Maximum distance the NPC wanders from its spawn position.")]
        public float WanderRadius = 3f;

        [Tooltip("Movement speed while wandering (units/second).")]
        public float MoveSpeed = 1f;

        [Tooltip("Ticks between picking a new wander destination.")]
        public int WanderIntervalTicks = 256;

        [Tooltip("Distance at which a player can begin interaction.")]
        public float InteractionRange = 1.5f;

        [Header("Collision")]
        [Tooltip("Radius used for Physics2D overlap checks in NonPlayerCharacterManager.")]
        public float ColliderRadius = 0.3f;

        [Header("Visuals")]
        [Tooltip("The non-networked visual prefab spawned by NonPlayerCharacterManager on all clients.")]
        public GameObject VisualPrefab;
        public RuntimeAnimatorController AnimatorController;
    }
}
