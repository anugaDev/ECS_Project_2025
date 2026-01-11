using System;
using Buildings;
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
        private GameObject _parent;

        public Action<SetPlayerUIActionComponent> OnClick;
        
        private SetPlayerUIActionComponent _componentData;

        public void Initialize(SetPlayerUIActionComponent componentData)
        {
            _componentData = componentData;
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
        }
        public void Disable()
        {
            _button.interactable = false;
        }
    }
}