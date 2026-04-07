using LichLord.Projectiles;
using LichLord.Buildables;
using LichLord.Props;
using LichLord.NonPlayerCharacters;
using System;
using UnityEngine;
using LichLord.Dialog;

namespace LichLord
{
    [Serializable]
    [CreateAssetMenu(fileName = "GlobalTables", menuName = "LichLord/Tables/GlobalTables")]
    public class GlobalTables : ScriptableObject
    {
        public ProjectileTable ProjectileTable;
        public PropTable PropTable;
        public BuildableTable BuildableTable;
        public NonPlayerCharacterTable NonPlayerCharacterTable;
        public ManeuverTable ManeuverTable;
        public InvasionTable InvasionTable;
        public CurrencyTable CurrencyTable;
        public DialogTable DialogTable;
        public ItemTable ItemTable;

        /*
        public ItemTable ItemTable;
        public MarkupPropTable MarkupPropTable;
        public HeroTable HeroTable;
        public MonsterTable MonsterTable;
        public TileScriptTable TileScriptTable;
        public GameplayEffectTable GameplayEffectTable;
        public ExecutionTable ExecutionTable;

        public ImpactTable ImpactTable;
        public EnchantmentTable EnchantmentTable;
        public StatNameTable StatNameTable;
        public ItemTypeTable ItemTypeTable;
        public LevelConfigTable LevelConfigTable;
        public LevelSequenceTable LevelSequenceTable;
        */
    }
}
