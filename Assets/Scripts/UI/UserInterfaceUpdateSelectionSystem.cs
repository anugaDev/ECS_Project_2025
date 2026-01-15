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
            foreach ((ElementSelectionComponent selectionComponent, UnitTypeComponent unitTypeComponent, Entity entity)
                     in SystemAPI.Query<ElementSelectionComponent, UnitTypeComponent>().WithEntityAccess())
            {
                CheckUnitSelection(selectionComponent, unitTypeComponent, entity);
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

        private void CheckUnitSelection(ElementSelectionComponent selectionComponent, UnitTypeComponent unitTypeComponent, Entity entity)
        {
            if (!selectionComponent.MustUpdateUI)
            {
                return;
            }

            selectionComponent.MustUpdateUI = false;
            SelectionEntity selectionEntity = GetNewSelectionEntity(_unitTypesSelected.Keys.ToList(), (int)unitTypeComponent.Type, entity);
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
            foreach ((ElementSelectionComponent selectionComponent, BuildingTypeComponent buildingTypeComponent, Entity entity)
                     in SystemAPI.Query<ElementSelectionComponent, BuildingTypeComponent>().WithEntityAccess())
            {
                CheckBuildingSelection(selectionComponent, buildingTypeComponent, entity);
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
            BuildingTypeComponent buildingTypeComponent, Entity entity)
        {
            if (!selectionComponent.MustUpdateUI)
            {
                return;
            }

            selectionComponent.MustUpdateUI = false;
            SelectionEntity selectionEntity = GetNewSelectionEntity(_buildingTypesSelected.Keys.ToList(), (int)buildingTypeComponent.Type, entity);
            _buildingTypesSelected.Add(selectionEntity, selectionComponent.IsSelected);
        }

        private SelectionEntity GetNewSelectionEntity(List<SelectionEntity> selectionEntities, int type, Entity entity)
        {
            int lastTypeId = -1;
            
            if (selectionEntities.Any(entity => entity.Type == type))
            {
                lastTypeId = selectionEntities.Last(entity => entity.Type == type).Id;
            }

            return new SelectionEntity(lastTypeId + 1, type, entity);
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
            SetDetailsDisplay();
        }

        private void SetDetailsDisplay()
        {
            if(_currentSelection is SelectableElementType.None)
            {
                SendEmptyDetails();
                return;
            }

            Entity detailsEntity = GetDetailsEntity();
            EntityCommandBuffer entityCommandBuffer = GetEntityCommandBuffer();
            Entity UIUpdateEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            entityCommandBuffer.AddComponent(UIUpdateEntity, GetDetailsComponent(detailsEntity));
        }

        private void SendEmptyDetails()
        {
            EntityCommandBuffer entityCommandBuffer = GetEntityCommandBuffer();
            Entity UIUpdateEntity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            entityCommandBuffer.AddComponent(UIUpdateEntity, new SetEmptyDetailsComponent());
        }

        private SetUIDisplayDetailsComponent GetDetailsComponent(Entity detailsEntity)
        {
            return new SetUIDisplayDetailsComponent
            {
                Entity = detailsEntity
            };
        }

        private Entity GetDetailsEntity()
        {
            if (_currentSelection is SelectableElementType.Building)
            {
                return _buildingTypesSelected.First().Key.SelectedEntity;
            }

            if (_unitTypesSelected.Any(unit => unit.Value && unit.Key.Type is (int)UnitType.Worker))
            {
                return _unitTypesSelected.First(unit => unit.Value && unit.Key.Type is (int)UnitType.Worker).Key.SelectedEntity;
            }

            return _unitTypesSelected.First().Key.SelectedEntity;
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