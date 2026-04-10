
namespace VoidRogues
{
    using System;
    using UnityEngine;
    using VoidRogues.NonPlayerCharacters;

    [Serializable]
    [CreateAssetMenu(fileName = "GlobalTables", menuName = "VoidRogues/Tables/GlobalTables")]
    public class GlobalTables : ScriptableObject
    {
        public NonPlayerCharacterTable NonPlayerCharacterTable;

        /*
        public ProjectileTable ProjectileTable;
        public PropTable PropTable;
        public BuildableTable BuildableTable;

        public ManeuverTable ManeuverTable;
        public InvasionTable InvasionTable;
        public CurrencyTable CurrencyTable;
        public ItemTable ItemTable;

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
