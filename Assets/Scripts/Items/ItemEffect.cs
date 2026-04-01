using UnityEngine;

namespace VoidRogues.Items
{
    /// <summary>
    /// Abstract base for all item runtime effects.
    /// Subclass this and attach to an item's effectPrefab.
    /// <see cref="Apply"/> is called once when the player picks up the item.
    /// </summary>
    public abstract class ItemEffect : MonoBehaviour
    {
        /// <summary>Apply this effect to the player GameObject.</summary>
        /// <param name="player">The player GameObject that picked up the item.</param>
        public abstract void Apply(GameObject player);
    }
}
