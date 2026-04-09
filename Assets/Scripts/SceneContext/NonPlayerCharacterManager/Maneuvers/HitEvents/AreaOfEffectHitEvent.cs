// using DWD.Utility.Loading; // TODO: Port from LichLord
// using LichLord.Projectiles; // TODO: Port from LichLord
// using LichLord.World; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "AreaOfEffectHitEvent", menuName = "VoidRogues/NonPlayerCharacters/HitEvents/AreaOfEffectHitEvent")]
    public class AreaOfEffectHitEvent : NonPlayerCharacterManeuverHitEvent
    {
        // TODO: Port BundleObject from LichLord
        // [BundleObject(typeof(GameObject))]
        // [SerializeField]
        // private BundleObject _hitEffect;
        // public BundleObject HitEffect => _hitEffect;

        // TODO: Port EMuzzle from LichLord
        // [SerializeField]
        // private EMuzzle[] _muzzles;
        // public EMuzzle[] Muzzles => _muzzles;

        [SerializeField]
        private float _aoeRadius;
        public float AoeRadius => _aoeRadius;

        [SerializeField]
        private float _aoeHeight;
        public float AoeHeight => _aoeHeight;

        [SerializeField]
        private int _damage;
        public int Damage => _damage;

        [SerializeField]
        protected LayerMask _hitCollisionLayer;
        public LayerMask HitCollisionLayer => _hitCollisionLayer;

        // TODO: Port Execute from LichLord (requires IChunkTrackable, IHitTarget, HurtboxOwner, etc.)
    }
}
