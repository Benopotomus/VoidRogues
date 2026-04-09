// using LichLord.Dialog; // TODO: Port from LichLord
using UnityEngine;

namespace VoidRogues
{
    public class NonPlayerCharacterDialogComponent : MonoBehaviour
    {
        [SerializeField] private int _dialogIndex;
        // TODO: Port DialogDefinition from LichLord
        // [SerializeField] private DialogDefinition _dialogDefinition;
        // public DialogDefinition CurrentDialog => _dialogDefinition;

        [SerializeField] private GameObject _indicator;

        private bool _shouldShowIndicator;

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            _dialogIndex = runtimeState.GetDialogIndex();
            // TODO: Port dialog indicator logic from LichLord
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            // TODO: Port dialog render update from LichLord
        }
    }
}
