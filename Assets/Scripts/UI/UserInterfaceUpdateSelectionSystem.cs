using System;
using System.Collections.Generic;
using System.Linq;
using Buildings;
using ElementCommons;
using Types;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace UI
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial class UserInterfaceUpdateSelectionSystem : SystemBase
    {
        private SelectableElementType _currentSelection;

        private Dictionary<SelectionEntity, bool> _unitTypesSelected;
        
        private Dictionary<SelectionEntity, bool> _buildingTypesSelected;

        private BuildingFactoryActionsFactory _buildingActionsFactory;

        private Dictionary<SelectableElementType, Action> _selectableToAction;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTagComponent>();
            _buildingActionsFactory = new BuildingFactoryActionsFactory();
            _buildingTypesSelected = new Dictionary<SelectionEntity, bool>();
            _unitTypesSelected = new Dictionary<SelectionEntity, bool>();
            FillSelectableDictionary();
            base.OnCreate();
        }

        private void FillSelectableDictionary()
        {
            _selectableToAction = new Dictionary<SelectableElementType, Action>
            {
                [SelectableElementType.Building] = SetBuildingActions,
                [SelectableElementType.Unit] = SetUnitActions,
                [SelectableElementType.None] = SetNoneSelected
            };
        }

        protected override void OnUpdate()
        {
            CheckUnitsSelection();
            SetBuildingsSelection();
            SetUIDetailsType();
            ResetSelectionData();
        }

        private void CheckUnitsSelection()
        {
            foreach ((ElementSelectionComponent selectionComponent, UnitTypeComponent unitTypeComponent)
                     in SystemAPI.Query<ElementSelectionComponent, UnitTypeComponent>())
            {
                CheckUnitSelection(selectionComponent, unitTypeComponent);
            }

            SetSelectionAsUnit();
        }

        private void SetSelectionAsUnit()
        {
            if (_unitTypesSelected.ContainsValue(true))
            {
                _currentSelection = SelectableElementType.Unit;
            }
            else if(_unitTypesSelected.Any())
            {
                _currentSelection = SelectableElementType.None;
            }
        }

        private void CheckUnitSelection(ElementSelectionComponent selectionComponent, UnitTypeComponent unitTypeComponent)
        {
            if (!selectionComponent.MustUpdateUI)
            {
                return;
            }

            selectionComponent.MustUpdateUI = false;
            SelectionEntity selectionEntity = GetNewSelectionEntity(_unitTypesSelected.Keys.ToList(), (int)unitTypeComponent.Type);
            _unitTypesSelected.Add(selectionEntity, selectionComponent.IsSelected);
        }

        private void SetBuildingsSelection()
        {
            if (_currentSelection == SelectableElementType.Unit)
            {
                return;
            }

            CheckBuildingsSelection();
        }

        private void CheckBuildingsSelection()
        {
            foreach ((ElementSelectionComponent selectionComponent, BuildingTypeComponent buildingTypeComponent)
                     in SystemAPI.Query<ElementSelectionComponent, BuildingTypeComponent>())
            {
                CheckBuildingSelection(selectionComponent, buildingTypeComponent);
            }

            SetSelectionAsBuilding();
        }

        private void SetSelectionAsBuilding()
        {
            if (_buildingTypesSelected.ContainsValue(true))
            {
                _currentSelection = SelectableElementType.Building;
            }
            else if (_buildingTypesSelected.Any())
            {
                _currentSelection = SelectableElementType.None;
            }
        }

        private void CheckBuildingSelection(ElementSelectionComponent selectionComponent,
            BuildingTypeComponent buildingTypeComponent)
        {
            if (!selectionComponent.MustUpdateUI)
            {
                return;
            }

            selectionComponent.MustUpdateUI = false;
            SelectionEntity selectionEntity = GetNewSelectionEntity(_buildingTypesSelected.Keys.ToList(), (int)buildingTypeComponent.Type);
            _buildingTypesSelected.Add(selectionEntity, selectionComponent.IsSelected);
        }

        private SelectionEntity GetNewSelectionEntity(List<SelectionEntity> selectionEntities, int type)
        {
            int lastTypeId = -1;
            
            if (selectionEntities.Any(entity => entity.Type == type))
            {
                lastTypeId = selectionEntities.Last(entity => entity.Type == type).Id;
            }

            return new SelectionEntity(lastTypeId + 1, type);
        }

        private void ResetSelectionData()
        {
            _currentSelection = SelectableElementType.Empty;
            _buildingTypesSelected.Clear();
            _unitTypesSelected.Clear();
        }

        private void SetUIDetailsType()
        {
            if (_currentSelection is SelectableElementType.Empty)
            {
                return;
            }

            SetUISelectionDetails();
        }

        private void SetUISelectionDetails()
        {
            _selectableToAction[_currentSelection]?.Invoke();
        }

        private void SetNoneSelected()
        {
            SetEmptyPayloadActionComponent(PlayerUIActionType.None);
        }

        private void SetUnitActions()
        {
            if (!_unitTypesSelected.Any(unit =>unit.Value && unit.Key.Type is (int)UnitType.Worker))
            {
                SetNoneSelected();
                return;
            }

            SetActionComponent(PlayerUIActionType.Build, GetBuildingsAsPayload());
        }

        private int[] GetBuildingsAsPayload()
        {
            return new[]
            {
                (int)BuildingType.Barracks,
                (int)BuildingType.Center,
                (int)BuildingType.House,
                (int)BuildingType.Farm
            };
        }

        private void SetBuildingActions()
        {
            _buildingActionsFactory.Set((BuildingType)_buildingTypesSelected.First().Key.Type);
            PlayerUIActionType action = _buildingActionsFactory.Get();
            int[] payload = _buildingActionsFactory.GetPayload(action);
            SetActionComponent(action, payload);
        }

        private void SetEmptyPayloadActionComponent(PlayerUIActionType action)
        {
            int[] emptyPayload = { -1 };
            SetActionComponent(action, emptyPayload);
        }

        private void SetActionComponent(PlayerUIActionType action, int[] payload)
        {
            EntityCommandBuffer entityCommandBuffer = GetEntityCommandBuffer();
            Entity UIUpdateEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            entityCommandBuffer.AddComponent(UIUpdateEntity, new UpdateUIActionTag());

            DynamicBuffer<UpdateUIActionPayload> updateUIActionPayloads =
                EntityManager.GetBuffer<UpdateUIActionPayload>(UIUpdateEntity);

            foreach (int payloadId in payload)
            {
                updateUIActionPayloads.Add(GetUpdateUIActioNPayload(action, payloadId));
            }

            entityCommandBuffer.Playback(EntityManager);
        }

        private static EntityCommandBuffer GetEntityCommandBuffer()
        {
            return new EntityCommandBuffer(Allocator.Temp);
        }

        private UpdateUIActionPayload GetUpdateUIActioNPayload(PlayerUIActionType action, int payloadId)
        {
            return new UpdateUIActionPayload
            {
                Action = action,
                PayloadID = payloadId
            };
        }
    }
}