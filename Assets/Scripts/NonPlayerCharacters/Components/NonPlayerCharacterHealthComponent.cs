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

        public void OnSpawned(NonPlayerCharacterRuntimeState state)
        {
            _currentHealth = state.GetHealth();
            _maxHealth = state.GetMaxHealth();
        }

        public void OnRender(NonPlayerCharacterRuntimeState state, int tick)
        {
            var oldHealth = _currentHealth;
            var newHealth = state.GetHealth();

            if (newHealth == _currentHealth)
                return;

            _currentHealth = newHealth;

            OnHealthChanged?.Invoke(oldHealth, _currentHealth, _maxHealth);
        }
    }
}
