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

        private void Awake()
        {
            _buttonActions  = new List<ActionButtonController>();
        }

        public void SetBuildingActions(BuildingsScriptableObject buildingConfigurations)
        {
            foreach (BuildingScriptableObject building in buildingConfigurations.GetBuildingsDictionary().Values)
            {
                ActionButtonController actionButton = Instantiate(_actionButtonPrefab, _parent);
                actionButton.Initialize(GetBuildingActionComponent(building.BuildingType), building.Name, building.Sprite);
                _buttonActions.Add(actionButton);
                actionButton.OnClick += SendActionComponent;
                actionButton.Hide();
            }
        }

        public void SetRecruitmentActions(UnitsScriptableObject unitsConfiguration)
        {
            foreach (UnitScriptableObject unit in unitsConfiguration.GetUnitsDictionary().Values)
            {
                ActionButtonController actionButton = Instantiate(_actionButtonPrefab, _parent);
                actionButton.Initialize(GetUnitsActionComponent(unit.UnitType), unit.Name, unit.Sprite);
                _buttonActions.Add(actionButton);
                actionButton.OnClick += SendActionComponent;
                actionButton.Hide();
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
            HashSet<(PlayerUIActionType action, int payload)> validActions = GetActionBufferAsHashSet(buffers);

            foreach (ActionButtonController actionButton in _buttonActions.Where(actionButton => validActions.Contains((
                         actionButton.GetActionType(),
                         actionButton.GetPayloadId()))))
            {
                actionButton.Show();
            }
        }

        private HashSet<(PlayerUIActionType action, int payload)> GetActionBufferAsHashSet(DynamicBuffer<UpdateUIActionPayload> buffers)
        {
            HashSet<(PlayerUIActionType action, int payload)> validActions = new();

            for (int i = 0; i < buffers.Length; i++)
            {
                validActions.Add((buffers[i].Action, buffers[i].PayloadID));
            }

            return validActions;
        }

        private void HideActions()
        {
            foreach (ActionButtonController buttonAction in _buttonActions)
            {
                buttonAction.Hide();
            }
        }

        public void EnableAction(EnableUIActionBuffer enableComponent)
        {
            foreach (ActionButtonController actionButton in
                     _buttonActions.Where(actionButton => enableComponent.PayloadID == actionButton.GetPayloadId()
                         && actionButton.GetActionType() == enableComponent.Action))
            {
                actionButton.Enable();
            }
        }
        public void DisableAction(DisableUIActionBuffer enableComponent)
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