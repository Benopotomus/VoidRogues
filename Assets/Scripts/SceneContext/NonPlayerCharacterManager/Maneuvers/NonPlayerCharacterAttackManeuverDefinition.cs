// using LichLord.Buildables; // TODO: Port from LichLord
// using LichLord.Props; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using System.Collections.Generic;
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "NPCManeuver", menuName = "VoidRogues/Maneuvers/NPCAttackManeuverDefinition", order = 1)]
    public class NonPlayerCharacterAttackManeuverDefinition : NonPlayerCharacterManeuverDefinition
    {
        public override EManeuverType ManeuverType => EManeuverType.Attack;

        [SerializeField]
        protected int _damage = 10;
        public int Damage => _damage;

        [SerializeField]
        private List<EManeuverTarget> _validTargetTypes = new List<EManeuverTarget>();
        public List<EManeuverTarget> ValidTargetTypes => _validTargetTypes;

        // TODO: Port FManeuverProjectile from LichLord
        // [Header("Projectiles")]
        // [SerializeField]
        // private List<FManeuverProjectile> _maneuverProjectiles = new List<FManeuverProjectile>();
        // public List<FManeuverProjectile> ManeuverProjectiles => _maneuverProjectiles;

        public override bool CanBeSelected(NonPlayerCharacterBrainComponent brainComponent, int tick)
        {
            if (!brainComponent.AttackTarget.HasTarget)
                return false;

            // TODO: Port full target validation from LichLord (requires IChunkTrackable, Lair, Prop, Buildable)
            return false;
        }
    }

    public enum EManeuverTarget
    {
        None,
        NPC,
        PC,
        Stronghold,
        Prop,
        Buildable,
        HarvestNode,
    }

    public enum EManeuverType
    {
        None,
        Attack,
        Harvest,
        Deposit,
    }
}
