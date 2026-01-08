using System;
using System.Collections.Generic;
using System.ComponentModel;
using Buildings;
using ScriptableObjects;
using Types;
using Unity.Entities;
using UnityEngine;

namespace UI.UIControllers
{
    public class ActionDisplayController : MonoBehaviour
    {
        [SerializeField] 
        private RectTransform _parent;

        [SerializeField] private ActionButtonController _actionButtonPrefab;
        
        private List<ActionButtonController> _buttonActions;
        
        public Action<IComponentData> OnActionSelected;

        public void SetBuildingActions(BuildingsScriptableObject buildings)
        {
            foreach (BuildingType buildingType in buildings.GetBuildingsDictionary().Keys)
            {
                ActionButtonController actionButton = Instantiate(_actionButtonPrefab, _parent);
                actionButton.Initialize(GetSetBuildingActionComponent(buildingType));
                _buttonActions.Add(actionButton);
                actionButton.OnClick += SendActionComponent;
            }
        }

        private SetBuildingActionComponent GetSetBuildingActionComponent(BuildingType buildingType)
        {
            return new SetBuildingActionComponent
            {
                BuildingType = buildingType
            };
        }

        private void SendActionComponent (IComponentData actionComponent)
        {
            OnActionSelected.Invoke(actionComponent);
        }

        private void OnDestroy()
        {
            foreach (ActionButtonController buttonController in _buttonActions)
            {
                buttonController.OnClick -= SendActionComponent;
            }
        }
    }
}