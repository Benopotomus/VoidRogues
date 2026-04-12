namespace VoidRogues
{
    using Fusion.Addons.KCC;
    using UnityEngine;
    using VoidRogues.NonPlayerCharacters;

    /// <summary>
    /// KCC processor that applies analytic horizontal depenetration against every
    /// active NPC by reading positions directly from the server-authoritative
    /// <see cref="NonPlayerCharacterManager"/> networked struct array.
    ///
    /// <b>Why this is fully deterministic</b><br/>
    /// NPC positions in <c>FNonPlayerCharacterData.Position</c> are written exclusively
    /// inside <c>NonPlayerCharacterManager.FixedUpdateNetwork</c> (Phase 1), which runs
    /// on every peer during Fusion's tick loop — including resimulated ticks on clients.
    /// Because Fusion restores the <c>[Networked]</c> array to the correct historical
    /// snapshot before each resimulated tick, Phase 1 integrates from the same starting
    /// state and produces the same positions on server and client alike.
    ///
    /// This processor therefore reads consistent, deterministic NPC positions whenever
    /// the KCC calls it during a resimulated move step, eliminating the prediction pops
    /// that occurred in the previous model where FollowerEntity (running in Unity's Update
    /// loop) moved NPCs outside Fusion's tick loop.
    ///
    /// <b>XZ-only distance</b><br/>
    /// This is a top-down game where all characters share the same ground plane (Y ≈ 0).
    /// Separation is computed purely in the horizontal (XZ) plane so that the KCC capsule's
    /// vertical extent does not inflate the measured distance and cause missed collisions.
    ///
    /// <b>Physics layer design:</b>
    /// NPC prefabs must be placed on the "NPC" physics layer (layer 6), which is intentionally
    /// excluded from the KCC's <c>CollisionLayerMask</c> (layer 0 / Default only).
    /// This ensures the KCC never contacts NPC physics capsules directly, so this processor
    /// is the <em>sole</em> player-NPC separation mechanism on both server and client.
    ///
    /// Add as a prefab processor in the KCC component's Processors list on the PlayerCharacter prefab.
    /// The manager reference is resolved lazily and retried every tick until found.
    /// </summary>
    public class NPCDepenetrationProcessor : KCCProcessor, IAfterMoveStep
    {
        // Run after EnvironmentProcessor (1000) and NoGravityXZMovementProcessor (1500)
        // so we apply a final position correction on top of fully-resolved movement.
        public const float Priority = -500f;
        public override float GetPriority(KCC kcc) => Priority;

        [SerializeField]
        [Tooltip("Collision radius for each NPC's logical shape (world units). " +
                 "Tune to match your NPC capsule/sphere visual size.")]
        private float _npcRadius = 0.4f;

        [SerializeField]
        [Tooltip("Extra separation gap added on top of the combined radius. " +
                 "Prevents tight sliding contact that can cause jitter.")]
        private float _skinWidth = 0.02f;

        [SerializeField]
        [Tooltip("Number of solver passes per KCC move step. " +
                 "More passes resolve simultaneous overlaps with several NPCs more accurately.")]
        [Range(1, 8)]
        private int _maxIterations = 3;

        private NonPlayerCharacterManager _npcManager;

        // NetworkBehaviour INTERFACE

        public override void Spawned()
        {
            base.Spawned();
            _npcManager = null;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            _npcManager = null;
        }

        // IAfterMoveStep INTERFACE

        public void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            if (_npcManager == null)
            {
                _npcManager = Object.FindFirstObjectByType<NonPlayerCharacterManager>();
                if (_npcManager == null)
                    return;
            }

            float combined   = kcc.Settings.Radius + _npcRadius + _skinWidth;
            float combinedSq = combined * combined;

            // Iterative solver so clustered NPCs all resolve cleanly.
            for (int iter = 0; iter < _maxIterations; iter++)
            {
                bool anyPenetration = false;

                // Iterate every slot in the fixed-size NPC array.
                // Slots with DefinitionID == 0 are free/recycled and are skipped cheaply.
                for (int i = 0; i < NonPlayerCharacterConstants.MAX_NPC_REPS; i++)
                {
                    ref FNonPlayerCharacterData npc = ref _npcManager.GetNpcData(i);

                    if (npc.DefinitionID == 0)
                        continue;

                    Vector3 npcPos = npc.Position;

                    // Use XZ-only distance so the KCC capsule's vertical extent does not
                    // inflate the measured separation and cause missed depenetrations.
                    float dx     = data.TargetPosition.x - npcPos.x;
                    float dz     = data.TargetPosition.z - npcPos.z;
                    float distSq = dx * dx + dz * dz;

                    if (distSq >= combinedSq)
                        continue;   // No overlap.

                    float dist = distSq > 1e-8f ? Mathf.Sqrt(distSq) : 0f;

                    // Build an XZ-only push direction (top-down game, no vertical push).
                    Vector3 pushDir;
                    if (dist > 1e-4f)
                    {
                        pushDir = new Vector3(dx / dist, 0f, dz / dist);
                    }
                    else
                    {
                        // Degenerate: player and NPC centres are coincident on XZ.
                        // Use a stable arbitrary horizontal direction as a fallback.
                        pushDir = Vector3.right;
                    }

                    float overlap = combined - dist;
                    data.TargetPosition += pushDir * overlap;

                    anyPenetration = true;
                }

                if (!anyPenetration)
                    break;
            }
        }
    }
}
