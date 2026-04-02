namespace VoidRogues.Combat
{
    /// <summary>
    /// Implemented by any entity that can receive damage (player, enemies, destructibles).
    /// The combat system uses this interface so it never needs a direct component reference.
    /// </summary>
    public interface IDamageable
    {
        bool IsAlive { get; }

        /// <summary>Apply the given damage amount to this entity.</summary>
        void ApplyDamage(int amount);
    }
}
