using System;

namespace VoidRogues
{
    [Serializable]
    public struct FAnimationTrigger
    {
        public int Action;
        public int Weapon;
        public int TriggerNumber;
        public int Side;
        public int Jumping;
        public int RightWeapon;
        public bool IsMoving;
        public bool IsBlocking;
        public float PlaybackSpeed;
    }
}
