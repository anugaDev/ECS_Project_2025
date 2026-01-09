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
        private EntitySelectionComponent _currentSelectionComponent;

        private NewSelectionComponent _currentNewSelection;

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
            foreach ((RefRW<LocalTransform> transform, NewSelectionComponent newSelection,
                         EntitySelectionComponent selection,Entity unitEntity)
                     in SystemAPI.Query<RefRW<LocalTransform>, NewSelectionComponent, EntitySelectionComponent>().WithEntityAccess()
                         .WithAll<Simulate>())
            {
                _currentNewSelection = newSelection;
                _currentSelectionComponent = selection;
                UpdateSelection(newSelection, transform, entityCommandBuffer, unitEntity,camera);
                entityCommandBuffer.RemoveComponent<NewSelectionComponent>(unitEntity);
            }
            
            entityCommandBuffer.Playback(state.EntityManager);
        }

        private void UpdateSelection(NewSelectionComponent selectPosition, RefRW<LocalTransform> transform,
            EntityCommandBuffer entityCommandBuffer, Entity unitEntity, Camera camera)
        {
            NewSelectionComponent newSelectPosition = selectPosition;
            UpdateUnitSelection(transform, camera);
            _currentSelectionComponent.MustUpdateUI = true;
            entityCommandBuffer.SetComponent(unitEntity, _currentSelectionComponent);
            entityCommandBuffer.SetComponent(unitEntity, newSelectPosition);
        }

        private void UpdateUnitSelection(RefRW<LocalTransform> transform, Camera camera)
        {
            Vector3 screenPos = camera.WorldToScreenPoint(transform.ValueRO.Position);

            if (_currentNewSelection.SelectionRect.Contains(screenPos))
            {
                UpdateSelected();
                return;
            }

            
            UpdateNotSelected();
        }

        private void UpdateSelected()
        {
            if (_currentNewSelection.MustKeepSelection && _currentSelectionComponent.IsSelected)
            {
                _currentSelectionComponent.IsSelected = false;
            }
            else
            {
                _currentSelectionComponent.IsSelected = true;
            }
        }

        private void UpdateNotSelected()
        {
            if (_currentNewSelection.MustKeepSelection)
            {
                return;
            }

            _currentSelectionComponent.IsSelected = false;
        }
    }
}