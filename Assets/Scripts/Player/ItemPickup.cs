using UnityEngine;
using VoidRogues.Items;

namespace VoidRogues.Player
{
    /// <summary>
    /// World-space item pickup. Triggers the inventory system when the player
    /// walks over it. Destroys itself after pickup.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class ItemPickup : MonoBehaviour
    {
        [SerializeField] private ItemDataSO item;
        [SerializeField] private SpriteRenderer iconRenderer;

        private void Start()
        {
            if (iconRenderer != null && item != null)
                iconRenderer.sprite = item.icon;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            var inventory = other.GetComponent<InventorySystem>();
            if (inventory == null)
                return;

            inventory.AddItem(item);
            Destroy(gameObject);
        }
    }
}
