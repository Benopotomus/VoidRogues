using System.Collections.Generic;
using UnityEngine;
using VoidRogues.Items;

namespace VoidRogues.Core
{
    /// <summary>
    /// Stores mutable state for the current run.
    /// A new RunData instance is created at the start of every run and
    /// discarded on player death or run completion.
    /// Never read run data from a ScriptableObject at runtime — use this class.
    /// </summary>
    public class RunData
    {
        // ── Player stats ──────────────────────────────────────────────────────
        public int CurrentSanity;
        public int MaxSanity;
        public float DamageMultiplier;
        public float FireRateMultiplier;
        public float MoveSpeed;
        public int Corruption;
        public int Fragments;

        // ── Progression ───────────────────────────────────────────────────────
        public int CurrentSector;
        public int RoomsCleared;
        public int TotalDamageDealt;
        public int EnemiesKilled;

        // ── Inventory ────────────────────────────────────────────────────────
        public List<ItemDataSO> CollectedItems = new List<ItemDataSO>();

        // ── Corruption thresholds ─────────────────────────────────────────────
        public const int CORRUPTION_TAINTED    = 25;
        public const int CORRUPTION_CORRUPTED  = 50;
        public const int CORRUPTION_VOID_TOUCHED = 75;
        public const int CORRUPTION_FULL_VOID  = 100;

        public CorruptionLevel GetCorruptionLevel()
        {
            if (Corruption >= CORRUPTION_FULL_VOID)   return CorruptionLevel.FullVoid;
            if (Corruption >= CORRUPTION_VOID_TOUCHED) return CorruptionLevel.VoidTouched;
            if (Corruption >= CORRUPTION_CORRUPTED)    return CorruptionLevel.Corrupted;
            if (Corruption >= CORRUPTION_TAINTED)      return CorruptionLevel.Tainted;
            return CorruptionLevel.Clean;
        }

        public RunData()
        {
            Reset();
        }

        /// <summary>Initialise all fields to their default run-start values.</summary>
        public void Reset()
        {
            CurrentSanity       = 100;
            MaxSanity           = 100;
            DamageMultiplier    = 1f;
            FireRateMultiplier  = 1f;
            MoveSpeed           = 5f;
            Corruption          = 0;
            Fragments           = 0;
            CurrentSector       = 1;
            RoomsCleared        = 0;
            TotalDamageDealt    = 0;
            EnemiesKilled       = 0;
            CollectedItems.Clear();
        }
    }

    public enum CorruptionLevel
    {
        Clean,
        Tainted,
        Corrupted,
        VoidTouched,
        FullVoid
    }
}
