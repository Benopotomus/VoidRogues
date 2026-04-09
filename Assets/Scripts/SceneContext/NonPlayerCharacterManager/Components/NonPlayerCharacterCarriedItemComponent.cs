// using LichLord.Items; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterCarriedItemComponent : MonoBehaviour
    {
        [SerializeField] private NonPlayerCharacter _npc;

        [SerializeField] private GameObject _stoneGO;
        [SerializeField] private GameObject _woodGO;
        [SerializeField] private GameObject _ironGO;
        [SerializeField] private GameObject _deathCapsGO;

        // TODO: Port FItemData and ItemDefinition from LichLord
        // private FItemData _carriedItem;
        // public FItemData CarriedItem => _carriedItem;

        // [SerializeField]
        // private ItemDefinition _definition;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            if (_stoneGO != null) _stoneGO.SetActive(false);
            if (_woodGO != null) _woodGO.SetActive(false);
            if (_ironGO != null) _ironGO.SetActive(false);
            if (_deathCapsGO != null) _deathCapsGO.SetActive(false);

            // TODO: Port carried item update from LichLord
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            // TODO: Port carried item render update from LichLord
        }
    }
}
