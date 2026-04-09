// using LichLord.Items; // TODO: Port from LichLord
// using LichLord.Projectiles; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterWeaponsComponent : MonoBehaviour
    {
        [SerializeField]
        private int _weaponIndex;

        // TODO: Port Weapon class from LichLord
        // [SerializeField]
        // private Weapon _weaponLeft;
        // [SerializeField]
        // private Weapon _weaponRight;

        [SerializeField]
        private Transform _handBoneLeft;

        [SerializeField]
        private Transform _handBoneRight;

        public void DropWeapons()
        {
        }

        public int GetWeaponID()
        {
            return _weaponIndex;
        }

        // TODO: Port GetMuzzlePosition from LichLord (requires Weapon, EMuzzle)
        // public Vector3 GetMuzzlePosition(EMuzzle muzzleName) { ... }
    }
}
