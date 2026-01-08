using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Buildings;
using ScriptableObjects;
using Types;
using Unity.Entities;
using UnityEngine;

namespace UI.UIControllers
{
    public class SelectionActionsDisplayController : MonoBehaviour
    {
        [SerializeField] 
        private RectTransform _parent;

        [SerializeField] private ActionButtonController _actionButtonPrefab;
        
        private List<ActionButtonController> _buttonActions;
        
        public Action<SetPlayerUIActionComponent> OnActionSelected;

        public void SetBuildingActions(BuildingsScriptableObject buildingConfigurations)
        {
            foreach (BuildingType buildingType in buildingConfigurations.GetBuildingsDictionary().Keys)
            {
                ActionButtonController actionButton = Instantiate(_actionButtonPrefab, _parent);
                actionButton.Initialize(GetBuildingActionComponent(buildingType));
                _buttonActions.Add(actionButton);
                actionButton.OnClick += SendActionComponent;
            }
        }
        public void SetRecruitmentActions(UnitsScriptableObject unitsConfiguration)
        {
            foreach (UnitType buildingType in unitsConfiguration.GetUnitsDictionary().Keys)
            {
                ActionButtonController actionButton = Instantiate(_actionButtonPrefab, _parent);
                actionButton.Initialize(GetUnitsActionComponent(buildingType));
                _buttonActions.Add(actionButton);
                actionButton.OnClick += SendActionComponent;
            }
        }
        
        private SetPlayerUIActionComponent GetBuildingActionComponent(BuildingType buildingType)
        {
            return new SetPlayerUIActionComponent
            {
                Action = PlayerUIActionType.Build,
                PayloadID = (int)buildingType
            };
        }
        private SetPlayerUIActionComponent GetUnitsActionComponent(UnitType buildingType)
        {
            return new SetPlayerUIActionComponent
            {
                Action = PlayerUIActionType.Recruit,
                PayloadID = (int)buildingType
            };
        }

        public void SetActionsActive(DynamicBuffer<UpdateUIActionPayload> buffers)
        {
            HideActions();
            foreach (ActionButtonController actionButton in _buttonActions.Where(actionButton => 
                         buffers.Any(buffer =>
                         actionButton.GetActionType() == buffer.Action &&
                         actionButton.GetPayloadId() == buffer.PayloadID)))
            {
                actionButton.Show();
            }
        }

        private void HideActions()
        {
            foreach (ActionButtonController buttonAction in _buttonActions)
            {
                buttonAction.Hide();
            }
        }

        public void EnableAction(EnableUIActionComponent enableComponent)
        {
            foreach (ActionButtonController actionButton in 
                     _buttonActions.Where(actionButton => enableComponent.PayloadID == actionButton.GetPayloadId() 
                         && actionButton.GetActionType() == enableComponent.Action))
            {
                actionButton.Enable();
            }
        }
        public void DisableAction(DisableUIActionComponent enableComponent)
        {
            foreach (ActionButtonController actionButton in 
                     _buttonActions.Where(actionButton => enableComponent.PayloadID == actionButton.GetPayloadId() 
                         && actionButton.GetActionType() == enableComponent.Action))
            {
                actionButton.Disable();
            }
        }

        private void SendActionComponent(SetPlayerUIActionComponent actionComponent)
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