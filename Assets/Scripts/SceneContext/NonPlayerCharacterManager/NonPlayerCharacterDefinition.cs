using UnityEngine;
using System.Collections.Generic;

namespace VoidRogues
{
    [CreateAssetMenu(fileName = "NonPlayerCharacterDefinition", menuName = "VoidRogues/NonPlayerCharacters/NonPlayerCharacterDefinition")]
    public class NonPlayerCharacterDefinition : ScriptableObject
    {
        [SerializeField]
        protected int _tableID;
        public int TableID => _tableID;

        [SerializeField]
        protected string _name;
        public string Name => _name;

        // Visuals
        [SerializeField]
        protected GameObject _prefab;
        public GameObject Prefab => _prefab;

        [SerializeField]
        protected Sprite _icon;
        public Sprite Icon => _icon;

        [SerializeField]
        protected int _maxHealth;
        public int MaxHealth => _maxHealth;

        [SerializeField]
        protected int _damageReduction = 3;
        public int DamageReduction => _damageReduction;

        [SerializeField]
        protected float _damageResistance = 0.0f;
        public float DamageResistance => _damageResistance;

        [SerializeField]
        protected int _hitReactThreshold = 3;
        public int HitReactThreshold => _hitReactThreshold;

        [SerializeField]
        protected float _walkSpeed;
        public float WalkSpeed => _walkSpeed;

        [SerializeField]
        protected bool _isFrontlineCombatant;
        public bool IsFrontlineCombatant => _isFrontlineCombatant;

        [Header("Weapons")]
        [SerializeField]
        protected int _weaponState;
        public int WeaponState => _weaponState;

        [SerializeField]
        protected int _weaponLeft;
        public int WeaponLeft => _weaponLeft;

        [SerializeField]
        protected int _weaponRight;
        public int WeaponRight => _weaponRight;

        [SerializeField]
        protected Vector2 _modelScale;
        public Vector2 ModelScale => _modelScale;

        [Header("Data Definitions Per Spawn Type")]
        [SerializeField]
        private SpawnTypeDataDefinitionEntry[] _spawnTypeDataDefinitions;

        public NonPlayerCharacterDataDefinition GetDataDefinition(ENPCSpawnType spawnType)
        {
            if (_spawnTypeDataDefinitions == null)
                return null;

            for (int i = 0; i < _spawnTypeDataDefinitions.Length; i++)
            {
                if (_spawnTypeDataDefinitions[i].SpawnType == spawnType)
                    return _spawnTypeDataDefinitions[i].DataDefinition;
            }

            return null;
        }
    }

    [System.Serializable]
    public struct SpawnTypeDataDefinitionEntry
    {
        public ENPCSpawnType SpawnType;
        public NonPlayerCharacterDataDefinition DataDefinition;
    }
}
