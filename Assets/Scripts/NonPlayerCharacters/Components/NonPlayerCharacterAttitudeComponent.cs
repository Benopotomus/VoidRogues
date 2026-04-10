using UnityEngine;

namespace VoidRogues.NonPlayerCharacters
{
    public class NonPlayerCharacterAttitudeComponent : MonoBehaviour
    {
        [SerializeField] private EAttitude _attitude;

        [Header("Material Settings")]
        [SerializeField] private Renderer _indicatorRenderer;
        [SerializeField] private string _colorProperty = "_Color";

        public void OnSpawned(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateAttitudeChange(runtimeState);
        }

        public void OnRender(NonPlayerCharacterRuntimeState runtimeState)
        {
            UpdateAttitudeChange(runtimeState);
        }

        private void UpdateAttitudeChange(NonPlayerCharacterRuntimeState runtimeState)
        {
            EAttitude oldAttitude = _attitude;
            EAttitude newAttitude = runtimeState.GetAttitude();

            if (oldAttitude == newAttitude)
                return;

            _attitude = newAttitude;

            Color targetColor = Color.white;

            switch (_attitude)
            {
                case EAttitude.Defensive:
                    targetColor = Color.yellow;
                    break;
                case EAttitude.Passive:
                    targetColor = Color.green;
                    break;
                case EAttitude.Hostile:
                    targetColor = Color.red;
                    break;
            }

            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.material.SetColor(_colorProperty, targetColor);
            }
        }
    }
}
