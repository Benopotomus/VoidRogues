using UnityEngine;
using VoidRogues.Core;

namespace VoidRogues.Items
{
    /// <summary>
    /// Concrete item effect: increases the player's damage multiplier.
    /// Attach to an item effectPrefab alongside ItemEffect.
    /// </summary>
    public class DamageBoostEffect : ItemEffect
    {
        [SerializeField] private float damageBonus = 0.15f;

        public override void Apply(GameObject player)
        {
            if (GameManager.Instance != null)
                GameManager.Instance.Run.DamageMultiplier += damageBonus;
        }
    }
}
