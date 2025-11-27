using Client;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct UnitSelectSystem : ISystem
    {
        private const float POSITION_THRESHOLD = 5f;

        private UnitSelectedComponent _selection;

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<LocalTransform> transform, SelectedPositionComponent selectPosition,
                         UnitSelectedComponent selection,Entity unitEntity)
                     in SystemAPI.Query<RefRW<LocalTransform>, SelectedPositionComponent, UnitSelectedComponent>().WithEntityAccess()
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
            if (math.distancesq(transform.ValueRO.Position, selectPositionValue) > POSITION_THRESHOLD)
            {
                _selection.Selected = false;
            }

            _selection.Selected = true;
        }
    }
}