using Client;
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
        private const float POSITION_THRESHOLD = 5f;

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
                UpdateUnitSelection(transform, selectPositionValue);
                entityCommandBuffer.SetComponent(unitEntity, _selection);
            }
            
            entityCommandBuffer.Playback(state.EntityManager);
        }

        private void UpdateUnitSelection(RefRW<LocalTransform> transform, float3 selectPositionValue)
        {
            Debug.Log(selectPositionValue);
            if (math.distancesq(transform.ValueRO.Position, selectPositionValue) > POSITION_THRESHOLD)
            {
                if(_selection.IsSelected) {Debug.Log("Set Unit Not Selected");} //TEST
                _selection.IsSelected = false;
                return;
            }

            _selection.IsSelected = true;
            Debug.Log("Set Unit selected");//TEST
        }
    }
}