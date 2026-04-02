namespace VoidRogues.Core
{
    // ── Player events ─────────────────────────────────────────────────────────

    /// <summary>Fired when the player's Sanity (HP) changes.</summary>
    public struct SanityChangedEvent
    {
        public int Current;
        public int Max;
    }

    /// <summary>Fired when the player dies (Sanity reaches zero).</summary>
    public struct PlayerDeathEvent
    {
        public UnityEngine.Vector2 LastPosition;
    }

    /// <summary>Fired when the player's Corruption level changes.</summary>
    public struct CorruptionChangedEvent
    {
        public int Current;
    }

    /// <summary>Fired when the player's Fragment (currency) count changes.</summary>
    public struct FragmentsChangedEvent
    {
        public int Current;
    }

    // ── Room / dungeon events ─────────────────────────────────────────────────

    /// <summary>Fired when all enemies in a room have been defeated.</summary>
    public struct RoomClearedEvent { }

    /// <summary>Fired when the player enters a new room.</summary>
    public struct RoomEnteredEvent
    {
        public RoomType RoomType;
    }

    // ── Combat events ─────────────────────────────────────────────────────────

    /// <summary>Fired when any damageable entity takes damage.</summary>
    public struct DamageDealtEvent
    {
        public UnityEngine.GameObject Target;
        public int Amount;
    }

    /// <summary>Fired when an enemy is killed.</summary>
    public struct EnemyKilledEvent
    {
        public UnityEngine.GameObject Enemy;
        public UnityEngine.Vector2 Position;
    }

    // ── Item events ───────────────────────────────────────────────────────────

    /// <summary>Fired when the player picks up an item.</summary>
    public struct ItemPickedUpEvent
    {
        public VoidRogues.Items.ItemDataSO Item;
    }
}
