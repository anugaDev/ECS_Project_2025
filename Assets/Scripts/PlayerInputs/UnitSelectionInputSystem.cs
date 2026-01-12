using Buildings;
using UI;
using UI.UIControllers;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PlayerInputs
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class UnitSelectionInputSystem : SystemBase
    {
        private const float CLICK_SELECTION_SIZE = 10f;

        private InputActions _inputActionMap;

        private Vector2 _startingPosition;

        private Vector2 _lastPosition;
        
        private bool _mustKeepSelection;
        
        private bool _isAvailable;

        private bool _isDragging;

        private CheckGameplayInteractionPolicy _interactionPolicy;

        protected override void OnCreate()
        {
            _interactionPolicy = new CheckGameplayInteractionPolicy();
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
            CheckInteractionAvailable();
            _isDragging = _isAvailable;
            EnableBoxSelection();
        }
        
        private void CheckInteractionAvailable()
        {
            foreach (SetPlayerUIActionComponent playerUIActionComponent in SystemAPI.Query<SetPlayerUIActionComponent>())
            {
                if (playerUIActionComponent.Action == PlayerUIActionType.Build)
                {
                    _isAvailable = false;
                }
            }

            _isAvailable = _interactionPolicy.IsAllowed();
        }

        private void EnableBoxSelection()
        {
            if (!_isDragging)
            {
                return;
            }

            UserInterfaceController.Instance.SelectionBoxController.Enable();
            _startingPosition = GetPointerPosition();
        }

        private void StartDragging()
        {
            bool dragStartedOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            _isDragging = !dragStartedOverUI;
        }

        private void UpdateBoxSelection()
        {
            if (!_isDragging)
            {
                return;
            }

            _mustKeepSelection = _inputActionMap.GameplayMap.KeepSelectionKey.IsPressed();
            _lastPosition = GetPointerPosition();
            UserInterfaceController.Instance.SelectionBoxController.UpdateBoxSize(_startingPosition, _lastPosition);
        }

        private void EndSelectionBox(InputAction.CallbackContext _)
        {
            _isDragging = false;
            UserInterfaceController.Instance.SelectionBoxController.Disable();
            SelectUnits();
        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    
        private Vector2 GetPointerPosition()
        {
            return _inputActionMap.GameplayMap.PointerPosition.ReadValue<Vector2>();
        }

        private void SelectUnits()
        {
            NormalizeSelectionClick();
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<OwnerTagComponent> _, Entity entity) in 
                     SystemAPI.Query<RefRO<OwnerTagComponent>>().WithEntityAccess())
            {
                ecb.AddComponent(entity, GetUnitPositionComponent());
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void NormalizeSelectionClick()
        {
            if (Vector2.Distance(_startingPosition, _lastPosition) < CLICK_SELECTION_SIZE)
            {
                _startingPosition -= Vector2.one * CLICK_SELECTION_SIZE;
                _lastPosition   += Vector2.one * CLICK_SELECTION_SIZE;
            }
        }

        private NewSelectionComponent GetUnitPositionComponent()
        {
            return new NewSelectionComponent
            {
                SelectionRect = GetBoxScreenRect(_startingPosition, _lastPosition),
                MustKeepSelection = _mustKeepSelection
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