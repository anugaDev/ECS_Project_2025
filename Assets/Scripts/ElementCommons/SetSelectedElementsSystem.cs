using ElementCommons;
using PlayerCamera;
using PlayerInputs;
using Types;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct SetSelectedElementsSystem : ISystem
    {
        private ElementSelectionComponent _currentSelectionComponent;

        private NewSelectionComponent _currentNewSelection;

        private SelectableElementTypeComponent _currentSelectedType;

        private ElementScreenRectCalculator _screenRectCalculator;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MainCameraTagComponent>();
            _screenRectCalculator = new ElementScreenRectCalculator();
        }

        public void OnUpdate(ref SystemState state)
        {
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            Camera camera = state.EntityManager
                .GetComponentObject<MainCameraComponentData>(cameraEntity) .Camera;
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<LocalTransform> transform, NewSelectionComponent newSelection, ElementSelectionComponent selection, 
                         SelectableElementTypeComponent selectedType, RefRO<PhysicsCollider> collider, Entity selectableEntity)
                     in SystemAPI.Query<RefRW<LocalTransform>, NewSelectionComponent, ElementSelectionComponent, SelectableElementTypeComponent, RefRO<PhysicsCollider>>
                             ().WithEntityAccess().WithAll<Simulate>())
            {
                _currentSelectedType = selectedType;
                _currentNewSelection = newSelection;
                _currentSelectionComponent = selection;
                UpdateSelection(newSelection, transform, collider, entityCommandBuffer, selectableEntity, camera);
                entityCommandBuffer.RemoveComponent<NewSelectionComponent>(selectableEntity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }

        private void UpdateSelection(NewSelectionComponent selectPosition, RefRW<LocalTransform> transform,
            RefRO<PhysicsCollider> collider, EntityCommandBuffer entityCommandBuffer, Entity unitEntity, Camera camera)
        {
            NewSelectionComponent newSelectPosition = selectPosition;
            bool currentSelectionState = _currentSelectionComponent.IsSelected;
            UpdateElementSelection(transform, collider, camera);
            _currentSelectionComponent.MustUpdateUI = currentSelectionState != _currentSelectionComponent.IsSelected;
            _currentSelectionComponent.MustEnableFeedback = _currentSelectionComponent.MustUpdateUI;
            entityCommandBuffer.SetComponent(unitEntity, _currentSelectionComponent);
            entityCommandBuffer.SetComponent(unitEntity, newSelectPosition);
        }

        private void UpdateElementSelection(RefRW<LocalTransform> transform, RefRO<PhysicsCollider> collider, Camera camera)
        {
            if (_currentSelectedType.Type is SelectableElementType.Building && !_currentNewSelection.IsClickSelection)
            {
                _currentSelectionComponent.IsSelected = false;
                return;
            }

            SelectElements(transform, collider, camera);

        }

        private void SelectElements(RefRW<LocalTransform> transform, RefRO<PhysicsCollider> collider, Camera camera)
        {
            Rect elementScreenRect = _screenRectCalculator.GetElementScreenRect(transform.ValueRO, collider.ValueRO, camera);

            if (_currentNewSelection.SelectionRect.Overlaps(elementScreenRect))
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