using Client;
using PlayerInputs;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct SetSelectedUnitSystem : ISystem
    {
        private const float POSITION_THRESHOLD = 1f;

        private UnitSelectionComponent _selection;

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<LocalTransform> transform, SelectedPositionComponent selectPosition,
                         UnitSelectionComponent selection,Entity unitEntity)
                     in SystemAPI.Query<RefRW<LocalTransform>, SelectedPositionComponent, UnitSelectionComponent>().WithEntityAccess()
                         .WithAll<Simulate>())
            {
                float3 selectPositionValue = selectPosition.Value;
                selectPositionValue.y = transform.ValueRO.Position.y;
                _selection = selection;
                UpdateSelection(selectPosition, transform, selectPositionValue, entityCommandBuffer, unitEntity);
            }
            
            entityCommandBuffer.Playback(state.EntityManager);
        }

        private void UpdateSelection(SelectedPositionComponent selectPosition, RefRW<LocalTransform> transform, float3 selectPositionValue,
            EntityCommandBuffer entityCommandBuffer, Entity unitEntity)
        {
            SelectedPositionComponent newSelectPosition = selectPosition;

            if (!newSelectPosition.MustUpdate)
            {
                return;
            }

            UpdateUnitSelection(transform, selectPositionValue);
            newSelectPosition.MustUpdate = false;
            entityCommandBuffer.SetComponent(unitEntity, _selection);
            entityCommandBuffer.SetComponent(unitEntity, newSelectPosition);
        }

        private void UpdateUnitSelection(RefRW<LocalTransform> transform, float3 selectPositionValue)
        {
            if (math.distancesq(transform.ValueRO.Position, selectPositionValue) > POSITION_THRESHOLD)
            {
                _selection.IsSelected = false;
                return;
            }

            _selection.IsSelected = true;
        }
    }
}