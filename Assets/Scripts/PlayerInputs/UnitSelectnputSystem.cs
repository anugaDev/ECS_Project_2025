using Client;
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
    public partial class UnitSelectInputSystem : SystemBase
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
            _inputActionMap.GameplayMap.SelectGameEntity.performed += OnSelectUnit;
        }
        protected override void OnStopRunning()
        {
            _inputActionMap.GameplayMap.SelectGameEntity.performed -= OnSelectUnit;
            _inputActionMap.Disable();
        }

        private void OnSelectUnit(InputAction.CallbackContext obj)
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            UnityEngine.Camera mainCamera = EntityManager.GetComponentObject<MainCameraComponentData>(cameraEntity).Camera;

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = DEFAULT_Z_POSITION;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);
            RaycastInput selectionInput = GetRaycastInput(worldPosition);
            SetUnitSelection(collisionWorld, selectionInput);
        }

        private void SetUnitSelection(CollisionWorld collisionWorld, RaycastInput selectionInput)
        {
            if (!collisionWorld.CastRay(selectionInput, out var closestHit))
            { 
                return;
            }

            Entity unitEntity = SystemAPI.GetSingletonEntity<OwnerTagComponent>();
            EntityManager.SetComponentData(unitEntity, GetUnitPositionComponent(closestHit));
        }

        private SelectedPositionComponent GetUnitPositionComponent(RaycastHit closestHit)
        {
            return new SelectedPositionComponent
            {
                Value = closestHit.Position 
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