namespace VoidRogues
{
    using Fusion.Addons.KCC;

    /// <summary>
    /// Retained as an empty KCC processor so existing prefab references remain valid.
    ///
    /// Player-NPC separation is now handled entirely server-side inside
    /// <see cref="VoidRogues.NonPlayerCharacters.NonPlayerCharacterManager.ApplyPlayerNPCSeparation"/>,
    /// which pushes NPC positions away from all player characters each tick.
    /// The player is intentionally never deflected by NPCs (Vampire Survivors style:
    /// players walk through enemies freely while enemies are pushed aside).
    ///
    /// NPC prefabs remain on the "NPC" physics layer (layer 6), which is excluded
    /// from the KCC's <c>CollisionLayerMask</c>, so the KCC never contacts NPC
    /// physics capsules directly.
    /// </summary>
    public class NPCDepenetrationProcessor : KCCProcessor, IAfterMoveStep
    {
        public override float GetPriority(KCC kcc) => -500f;

        public void Execute(AfterMoveStep stage, KCC kcc, KCCData data)
        {
            // Intentionally empty.  NPCs are pushed away from the player server-side;
            // the player itself is never blocked by NPCs.
        }
    }
}
