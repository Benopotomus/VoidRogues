using UnityEngine;

namespace VoidRogues.Items
{
    public enum ItemTier
    {
        Common,
        Uncommon,
        Rare,
        Cursed,
        Void
    }

    /// <summary>
    /// ScriptableObject definition for a single item.
    /// One asset per item in the game, stored in Assets/Resources/Items/.
    /// Never mutate at runtime — it is shared across all runs.
    /// </summary>
    [CreateAssetMenu(fileName = "ItemData_New", menuName = "VoidRogues/Item Data")]
    public class ItemDataSO : ScriptableObject
    {
        [Header("Identity")]
        public string itemName;
        [TextArea(2, 4)]
        public string description;
        [TextArea(1, 2)]
        public string loreText;
        public Sprite icon;

        [Header("Tier")]
        public ItemTier tier;

        [Header("Cost")]
        public int fragmentCost;    // Shop purchase price (0 = not sold in shop)

        [Header("Corruption")]
        [Tooltip("How much Corruption this item adds when collected.")]
        public int corruptionCost;

        [Header("Runtime Effect")]
        [Tooltip("Prefab with an ItemEffect component that is instantiated on pickup.")]
        public GameObject effectPrefab;
    }
}
