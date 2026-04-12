using System;
using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterHealthComponent : MonoBehaviour
    {
        [SerializeField]
        private NonPlayerCharacter _npc;

        [SerializeField]
        private int _currentHealth;
        public int CurrentHealth => _currentHealth;

        [SerializeField]
        private int _maxHealth;
        public int MaxHealth => _maxHealth;

        public Action<int, int, int> OnHealthChanged;

        public void OnSpawned(ref FNonPlayerCharacterData data)
        {
            //_currentHealth = data.GetHealth();
           // _maxHealth = data.GetMaxHealth();
        }

        public void OnRender(ref FNonPlayerCharacterData toData, ref FNonPlayerCharacterData fromData,
            float alpha, float renderTime, float networkDeltaTime, float localDeltaTime, int tick)
        {

        }
    }
}
