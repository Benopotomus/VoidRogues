// using LichLord.Buildables; // TODO: Port from LichLord
// using LichLord.Items; // TODO: Port from LichLord
// using LichLord.Props; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "VoidRogues/Maneuvers/NPCDepositManeuverDefinition", order = 1)]
    public class NonPlayerCharacterDepositManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Deposit;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            // TODO: Port deposit target validation from LichLord (requires Stockpile, ContainerManager, FItemData)
            return false;
        }
    }
}
