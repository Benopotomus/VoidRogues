// using LichLord.Props; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "VoidRogues/Maneuvers/NPCHarvestManeuverDefinition", order = 1)]
    public class NonPlayerCharacterHarvestManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Harvest;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            // TODO: Port harvest target validation from LichLord (requires HarvestNode, IChunkTrackable)
            return false;
        }
    }
}
