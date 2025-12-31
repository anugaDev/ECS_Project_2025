using PlayerInputs.SelectionBox;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.Input;

namespace PlayerInputs
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class UnitSelectionInputSystem : SystemBase
    {
        private InputActions _inputActionMap;

        private Vector2 _startingPosition;

        private Vector2 _lastPosition;

        private bool _isDragging;

        protected override void OnCreate()
        {
            _inputActionMap = new InputActions();
            RequireForUpdate<OwnerTagComponent>();
            RequireForUpdate<NetworkTime>();
        }

        protected override void OnStartRunning()
        {
            _inputActionMap.Enable();
            _inputActionMap.GameplayMap.SelectGameEntity.started += StartBoxSelection;
            _inputActionMap.GameplayMap.SelectGameEntity.canceled += EndSelectionBox;
        }
        
        protected override void OnStopRunning()
        {
            _inputActionMap.GameplayMap.SelectGameEntity.started -= StartBoxSelection;
            _inputActionMap.GameplayMap.SelectGameEntity.canceled -= EndSelectionBox;
            _inputActionMap.Disable();
        }

        private void StartBoxSelection(InputAction.CallbackContext _)
        {
            _isDragging = true;
            SelectionBoxController.Instance.Enable();
            _startingPosition = GetPointerPosition();
        }

        private void UpdateBoxSelection()
        {
            if (!_isDragging)
            {
                return;
            }

            _lastPosition = GetPointerPosition();
            SelectionBoxController.Instance.UpdateBoxSize(_startingPosition, _lastPosition);
        }
        
        private void EndSelectionBox(InputAction.CallbackContext _)
        {
            _isDragging = false;
            SelectionBoxController.Instance.Disable();
            SelectUnits();
        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
        private Vector2 GetPointerPosition()
        {
            return _inputActionMap.GameplayMap.PointerPosition.ReadValue<Vector2>();
        }

        private void SelectUnits()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<OwnerTagComponent> _, Entity entity) in 
                     SystemAPI.Query<RefRO<OwnerTagComponent>>().WithEntityAccess())
            { 
                ecb.AddComponent(entity, GetUnitPositionComponent());
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private SelectionBoxPositionComponent GetUnitPositionComponent()
        {
            Vector2 convertedStartingPosition = SelectionBoxController.Instance.ScreenToCanvas(_startingPosition);
            Vector2 convertedLastPosition = SelectionBoxController.Instance.ScreenToCanvas(_lastPosition);

            return new SelectionBoxPositionComponent
            {
                Value = GetBoxScreenRect(convertedStartingPosition, convertedLastPosition),
            };
        }

        private Rect GetBoxScreenRect(Vector2 startingPosition, Vector2 endingPosition)
        {
            Vector2 min = Vector2.Min(startingPosition, endingPosition);
            Vector2 max = Vector2.Max(startingPosition, endingPosition);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        protected override void OnUpdate()
        {
            UpdateBoxSelection();
        }
    }
}