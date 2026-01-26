using System;
using Buildings;
using TMPro;
using Types;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace UI.UIControllers
{
    public class ActionButtonController : MonoBehaviour
    {
        [SerializeField]
        private Button _button;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private Image _displayImage;

        [SerializeField]
        private Image _feedbackImage;

        [SerializeField]
        private GameObject _parent;

        [SerializeField] 
        private Color _enabledColor;

        [SerializeField]
        private Color _disabledColor;

        public Action<SetPlayerUIActionComponent> OnClick;
        
        private SetPlayerUIActionComponent _componentData;

        public void Initialize(SetPlayerUIActionComponent componentData, string elementName, Sprite elementSprite)
        {
            _componentData = componentData;
            _text.text = elementName;
            _displayImage.sprite = elementSprite;
            _button.onClick.AddListener(SendAction);
        }

        public PlayerUIActionType GetActionType()
        {
            return _componentData.Action;
        }

        public int GetPayloadId()
        {
            return _componentData.PayloadID;
        }

        private void SendAction()
        {
            OnClick.Invoke(_componentData);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }

        public void Show()
        {
            _parent.SetActive(true);
        }

        public void Hide()
        {
            _parent.SetActive(false);
        }

        public void Enable()
        {
            _button.interactable = true;
            _feedbackImage.color = _enabledColor;
        }

        public void Disable()
        {
            _button.interactable = false;
            _feedbackImage.color = _disabledColor;
        }
    }
}