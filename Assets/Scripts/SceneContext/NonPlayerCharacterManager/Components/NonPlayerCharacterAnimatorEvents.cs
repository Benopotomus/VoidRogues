using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterAnimatorEvents : MonoBehaviour
    {
        public void FootR()
        { }

        public void FootL()
        { }

        public void Hit()
        {
            // _npc.Brain.OnHitFromAnimation();
        }

        public void Special()
        {
            // _npc.Brain.OnSpecialEventFromAnimation();
        }

        public void HitSweep(bool isSweeping)
        {
            // _npc.Brain.OnSweepChangeFromAnimation(isSweeping);
        }

        public void Land()
        { }

        public void Shoot()
        { }

        public void Shoot(float shooting, int shooting1, object shootObject)
        { }

        public void Shoot(object shootObject)
        { }

        public void Shoot(int shooting)
        { }
    }
}
