using System.Collections.Generic;
using UnityEngine;
using VoidRogues.Core;
using VoidRogues.Items;

namespace VoidRogues.Player
{
    /// <summary>
    /// Holds the player's collected items for the current run.
    /// Applies item effects on pickup and notifies RunData.
    /// </summary>
    public class InventorySystem : MonoBehaviour
    {
        private readonly List<ItemDataSO> _items = new List<ItemDataSO>();
        private readonly List<Items.ItemEffect> _activeEffects = new List<Items.ItemEffect>();

        public IReadOnlyList<ItemDataSO> Items => _items;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Pick up an item: apply its effect and record it in RunData.</summary>
        public void AddItem(ItemDataSO item)
        {
            _items.Add(item);

            // Apply corruption cost to run data
            if (GameManager.Instance != null)
            {
                GameManager.Instance.Run.CollectedItems.Add(item);
                AddCorruption(item.corruptionCost);
            }

            // Instantiate and activate the item's runtime effect
            if (item.effectPrefab != null)
            {
                var effectGO = Instantiate(item.effectPrefab, transform);
                var effect = effectGO.GetComponent<Items.ItemEffect>();
                if (effect != null)
                {
                    effect.Apply(gameObject);
                    _activeEffects.Add(effect);
                }
            }

            EventBus.Publish(new ItemPickedUpEvent { Item = item });
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void AddCorruption(int amount)
        {
            if (amount <= 0 || GameManager.Instance == null)
                return;

            GameManager.Instance.Run.Corruption =
                Mathf.Min(RunData.CORRUPTION_FULL_VOID, GameManager.Instance.Run.Corruption + amount);

            EventBus.Publish(new CorruptionChangedEvent
            {
                Current = GameManager.Instance.Run.Corruption
            });
        }
    }
}
