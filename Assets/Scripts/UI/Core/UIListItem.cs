using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoidRogues.UI
{
    public sealed class UIListItem : UIListItemBase<MonoBehaviour>, ISelectHandler
    {
    }

    public abstract class UIListItemBase<T> : UIBehaviour where T : MonoBehaviour
    {
        // PUBLIC MEMBERS

        public int ID { get; set; }
        public T Content => _content;
        public bool IsSelectable => _button != null;
        public bool IsSelected { get { return _isSelected; } set { SetIsSelected(value); } }
        public bool IsHovered { get { return _isHovered; } set { SetIsHovered(value); } }

        public bool IsInteractable { get { return GetIsInteractable(); } set { SetIsInteractable(value); } }

        public Action<int> Clicked;
        public Action<int> Hovered;

        // PRIVATE MEMBERS

        [SerializeField]
        private Button _button;
        [SerializeField]
        private T _content;
        [SerializeField]
        private CanvasGroup _selectedGroup;
        [SerializeField]
        private CanvasGroup _deselectedGroup;

        private bool _isSelected;
        private bool _isHovered;

        // MONOBEHAVIOR

        protected virtual void Awake()
        {
            SetIsSelected(false, true);

            if (_button != null)
            {
                _button.onClick.AddListener(OnClick);
                
            }
        }

        protected virtual void OnDestroy()
        {
            Clicked = null;

            if (_button != null)
            {
                _button.onClick.RemoveListener(OnClick);
            }
        }

        // PRIVATE METHODS

        private void SetIsSelected(bool value, bool force = false)
        {
            if (_isSelected == value && force == false)
                return;

            _isSelected = value;

            _selectedGroup.SetVisibility(value);
            _deselectedGroup.SetVisibility(value == false);
        }

        private void SetIsHovered(bool value, bool force = false)
        {
            if (_isHovered == value && force == false)
                return;

            _isHovered = value;
        }

        private bool GetIsInteractable()
        {
            return _button != null ? _button.interactable : false;
        }

        private void SetIsInteractable(bool value)
        {
            if (_button == null)
                return;

            _button.interactable = value;
        }

        private void OnClick()
        {
            Clicked?.Invoke(ID);
        }

        public void OnSelect(BaseEventData eventData)
        {
            Hovered?.Invoke(ID);
        }
    }
}
