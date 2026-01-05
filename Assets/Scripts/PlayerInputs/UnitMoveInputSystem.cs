using PlayerCamera;
using PlayerInputs.MoveIndicator;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using Input = UnityEngine.Input;
using RaycastHit = Unity.Physics.RaycastHit;

namespace PlayerInputs
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class UnitMoveInputSystem : SystemBase
    {
        private const uint GROUNDPLANE_GROUP = 1 << 0; 
        
        private const uint RAYCAST_GROUP = 1 << 5; 
        
        private const float DEFAULT_Z_POSITION = 100f; 
        
        private CollisionFilter _selectionFilter;
        
        private InputActions _inputActionMap;

        private bool _anySelected;

        private UnitTargetPositionComponent _unitTargetPositionComponent;

        private MoveIndicatorController _moveIndicator;

        private bool _indicatorIsSet;

        protected override void OnCreate()
        {
            _inputActionMap = new InputActions();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = RAYCAST_GROUP,
                CollidesWith = GROUNDPLANE_GROUP
            };
            
            RequireForUpdate<OwnerTagComponent>();
            RequireForUpdate<MoveIndicatorPrefabComponent>();
        }

        protected override void OnStartRunning()
        {
            _inputActionMap.Enable();
            _inputActionMap.GameplayMap.SelectMovePosition.performed += OnSelectMovePosition;
        }

        private void SetMoveIndicator()
        {
            MoveIndicatorController moveIndicatorPrefab = SystemAPI.ManagedAPI.GetSingleton<MoveIndicatorPrefabComponent>().Value;
            _moveIndicator = Object.Instantiate(moveIndicatorPrefab);
            _indicatorIsSet = true;
        }

        protected override void OnStopRunning()
        {
            _inputActionMap.GameplayMap.SelectMovePosition.performed -= OnSelectMovePosition;
            _inputActionMap.Disable();
        }

        private void OnSelectMovePosition(InputAction.CallbackContext obj)
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            Camera mainCamera = EntityManager.GetComponentObject<MainCameraComponentData>(cameraEntity).Camera;
            RaycastInput selectionInput = GetRaycastInput(mainCamera);
            SetUnitPosition(collisionWorld, selectionInput);
        }

        private void SetUnitPosition(CollisionWorld collisionWorld, RaycastInput selectionInput)
        {
            if (!collisionWorld.CastRay(selectionInput, out var closestHit))
            { 
                return;
            }

            foreach ((RefRO<OwnerTagComponent> _, Entity entity) in 
                     SystemAPI.Query<RefRO<OwnerTagComponent>>().WithEntityAccess())
            {
                SetSelectedUnitPosition(closestHit, entity);
            }

            SetMovePositionIndicator();
        }

        private void SetMovePositionIndicator()
        {
            if (!_anySelected)
            {
                return;
            }

            if (!_indicatorIsSet)
            {
                SetMoveIndicator();
            }

            float3 spawnPosition = _unitTargetPositionComponent.Value;
            _moveIndicator.Set(spawnPosition);
            _anySelected = false;
        }

        private void SetSelectedUnitPosition(RaycastHit closestHit, Entity entity)
        {
            UnitSelectionComponent selectedPositionComponent = EntityManager.GetComponentData<UnitSelectionComponent>(entity);

            if (!selectedPositionComponent.IsSelected)
            {
                return;
            }

            _anySelected = true;
            _unitTargetPositionComponent = GetUnitPositionComponent(closestHit);
            EntityManager.SetComponentData(entity, _unitTargetPositionComponent);
        }

        private UnitTargetPositionComponent GetUnitPositionComponent(RaycastHit closestHit)
        {
            return new UnitTargetPositionComponent
            {
                Value = closestHit.Position,
                MustMove =  true
            };
        }

        private RaycastInput GetRaycastInput(Camera mainCamera)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = DEFAULT_Z_POSITION;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

            return new RaycastInput
            {
                Start = mainCamera.transform.position,
                End = worldPosition,
                Filter = _selectionFilter,
            };
        }

        protected override void OnUpdate()
        {
        }
    }
}