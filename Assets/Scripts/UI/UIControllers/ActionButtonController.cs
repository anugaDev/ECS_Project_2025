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

        public Action<IComponentData> OnClick;
        
        private IComponentData _componentData;

        public void Initialize(IComponentData componentData)
        {
            _componentData = componentData;
            _button.onClick.AddListener(SendAction);
        }

        private void SendAction()
        {
            OnClick.Invoke(_componentData);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveAllListeners();
        }
    }
}