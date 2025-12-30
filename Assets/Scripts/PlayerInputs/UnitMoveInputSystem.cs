using PlayerCamera;
using Units;
using Unity.Entities;
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
        
        protected override void OnCreate()
        {
            _inputActionMap = new InputActions();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = RAYCAST_GROUP,
                CollidesWith = GROUNDPLANE_GROUP
            };
            
            RequireForUpdate<OwnerTagComponent>();
        }

        protected override void OnStartRunning()
        {
            _inputActionMap.Enable();
            _inputActionMap.GameplayMap.SelectMovePosition.performed += OnSelectMovePosition;
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
            UnityEngine.Camera mainCamera = EntityManager.GetComponentObject<MainCameraComponentData>(cameraEntity).Camera;

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = DEFAULT_Z_POSITION;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            RaycastInput selectionInput = GetRaycastInput(worldPosition);
            SetUnitPosition(collisionWorld, selectionInput);
        }

        private void SetUnitPosition(CollisionWorld collisionWorld, RaycastInput selectionInput)
        {
            if (!collisionWorld.CastRay(selectionInput, out var closestHit))
            { 
                return;
            }

            Entity unitEntity = SystemAPI.GetSingletonEntity<OwnerTagComponent>();
            EntityManager.SetComponentData(unitEntity, GetUnitPositionComponent(closestHit));
        }

        private UnitTargetPositionComponent GetUnitPositionComponent(RaycastHit closestHit)
        {
            return new UnitTargetPositionComponent
            {
                Value = closestHit.Position,
                MustMove =  true
            };
        }

        private RaycastInput GetRaycastInput(Vector3 worldPosition)
        {
            return new RaycastInput
            {
                Start = worldPosition,
                End = worldPosition,
                Filter = _selectionFilter,
            };
        }

        protected override void OnUpdate()
        {
        }
    }
}