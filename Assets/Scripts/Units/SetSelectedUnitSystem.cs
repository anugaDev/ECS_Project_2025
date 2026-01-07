using Client;
using PlayerCamera;
using PlayerInputs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct SetSelectedUnitSystem : ISystem
    {
        private UnitSelectionComponent _selection;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainCameraTagComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            Camera camera = state.EntityManager
                .GetComponentObject<MainCameraComponentData>(cameraEntity) .Camera;
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<LocalTransform> transform, SelectionBoxPositionComponent selectPosition,
                         UnitSelectionComponent selection,Entity unitEntity)
                     in SystemAPI.Query<RefRW<LocalTransform>, SelectionBoxPositionComponent, UnitSelectionComponent>().WithEntityAccess()
                         .WithAll<Simulate>())
            {
                Rect selectPositionValue = selectPosition.Value;
                _selection = selection;
                UpdateSelection(selectPosition, transform, selectPositionValue, entityCommandBuffer, unitEntity,camera);
                entityCommandBuffer.RemoveComponent<SelectionBoxPositionComponent>(unitEntity);
            }
            
            entityCommandBuffer.Playback(state.EntityManager);
        }

        private void UpdateSelection(SelectionBoxPositionComponent selectPosition, RefRW<LocalTransform> transform,
            Rect selectPositionValue,
            EntityCommandBuffer entityCommandBuffer, Entity unitEntity, Camera camera)
        {
            SelectionBoxPositionComponent newSelectPosition = selectPosition;
            UpdateUnitSelection(transform, selectPositionValue, camera);
            entityCommandBuffer.SetComponent(unitEntity, _selection);
            entityCommandBuffer.SetComponent(unitEntity, newSelectPosition);
        }

        private void UpdateUnitSelection(RefRW<LocalTransform> transform, Rect selectPositionValue, Camera camera)
        {
            Vector3 screenPos = camera.WorldToScreenPoint(transform.ValueRO.Position);

            if (selectPositionValue.Contains(screenPos))
            {
                _selection.IsSelected = true;
                return;
            }
            
            _selection.IsSelected = false;
        }
    }
}