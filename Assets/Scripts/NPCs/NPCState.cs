using Fusion;
using UnityEngine;

namespace VoidRogues.NPCs
{
    /// <summary>
    /// Blittable struct for a single NPC's networked state.
    ///
    /// Stored in <see cref="NPCManager._npcs"/> (a <see cref="NetworkArray{T}"/>).
    /// The host writes this struct every tick; clients read it via <see cref="ChangeDetector"/>.
    /// </summary>
    public struct NPCState : INetworkStruct
    {
        /// <summary>True while this slot has a live NPC.</summary>
        public NetworkBool IsActive;

        /// <summary>World-space position.</summary>
        public Vector2 Position;

        /// <summary>Current velocity (units/second).</summary>
        public Vector2 Velocity;

        /// <summary>Index into <see cref="NPCManager._npcDatabase"/>.</summary>
        public byte TypeIndex;

        /// <summary>
        /// Animation state: 0 = Idle, 1 = Walk, 2 = Talk, 3 = Interact.
        /// Clients drive the <c>Animator</c> from this value.
        /// </summary>
        public byte AnimState;

        /// <summary>
        /// Dialogue state: 0 = None, 1 = Greeting, 2 = InDialogue, 3 = Farewell.
        /// Used by clients to drive UI prompts and dialogue panels.
        /// </summary>
        public byte DialogueState;

        /// <summary>
        /// <see cref="PlayerRef.AsIndex"/> of the player currently interacting
        /// with this NPC, or -1 if no interaction is active.
        /// </summary>
        public int InteractingPlayer;

        /// <summary>
        /// Tick on which the current wander destination was set.
        /// Used by the AI to decide when to pick a new target.
        /// </summary>
        public int WanderStartTick;

        /// <summary>Target position for wander behaviour.</summary>
        public Vector2 WanderTarget;
    }
}
