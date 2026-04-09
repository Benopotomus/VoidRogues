// using DWD.Utility.Loading; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "SingleTargetHitEvent", menuName = "VoidRogues/NonPlayerCharacters/HitEvents/SingleTargetHitEvent")]
    public class SingleTargetHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        // TODO: Port BundleObject from LichLord
        // [BundleObject(typeof(GameObject))]
        // [SerializeField]
        // private BundleObject _hitEffect;
        // public BundleObject HitEffect => _hitEffect;

        // TODO: Port Execute from LichLord (requires IChunkTrackable, IHitTarget, HitUtility, etc.)
    }
}
