using System.Collections.Generic;
using Buildings;
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
        private bool _unitSelection;

        private bool _buildingSelection;

        private List<UnitType> _unitTypesSelected;

        private BuildingType _buildingTypeSelected;

        private BuildingFactoryActionsFactory _buildingActionsFactory;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }

        protected override void OnUpdate()
        {
            _unitTypesSelected = new List<UnitType>();
            
            foreach ((EntitySelectionComponent selectionComponent, UnitTypeComponent unitTypeComponent)
                     in SystemAPI.Query<EntitySelectionComponent, UnitTypeComponent>())
            {
                if (!selectionComponent.MustUpdateUI)
                {
                    continue;
                }

                _unitSelection = true;
                _buildingSelection = true;
                _unitTypesSelected.Add(unitTypeComponent.Type);
            }

            if (!_unitSelection)
            {
                foreach ((EntitySelectionComponent selectionComponent, BuildingTypeComponent buildingTypeComponent)
                         in SystemAPI.Query<EntitySelectionComponent, BuildingTypeComponent>())
                {
                    if (!selectionComponent.MustUpdateUI)
                    {
                        continue;
                    }

                    _buildingSelection = true;
                    _buildingTypeSelected = buildingTypeComponent.Type;
                }
            }

            SetUIDetailsType();
            ResetSelectionData();
        }

        private void ResetSelectionData()
        {
            _unitSelection = false;
            _buildingSelection = false;
            _unitTypesSelected.Clear();
        }

        private void SetUIDetailsType()
        {
            if (_unitSelection)
            {
                SetUnitActions();
            }

            else if (_buildingSelection)
            {
                SetBuildingActions();
            }
        }

        private void SetUnitActions()
        {
            if (!_unitTypesSelected.Contains(UnitType.Worker))
            {
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
            _buildingActionsFactory.Set(_buildingTypeSelected);
            PlayerUIActionType action = _buildingActionsFactory.Get();
            int[] payload = _buildingActionsFactory.GetPayload(action);
            SetActionComponent(action, payload);
        }

        private void SetActionComponent(PlayerUIActionType action, int[] payload)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            Entity UIUpdateEntity = SystemAPI.GetSingletonEntity<PlayerUIActionsTagComponent>();
            entityCommandBuffer.AddComponent(UIUpdateEntity, new UpdateUIActionTag());

            DynamicBuffer<UpdateUIActionPayload> updateUIActionPayloads =
                EntityManager.GetBuffer<UpdateUIActionPayload>(UIUpdateEntity);

            foreach (int payloadId in payload)
            {
                updateUIActionPayloads.Add(GetUpdateUIActioNPayload(action, payloadId));
            }

            entityCommandBuffer.Playback(EntityManager);
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