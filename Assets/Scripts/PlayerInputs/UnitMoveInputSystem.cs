using Buildings;
using ElementCommons;
using GatherableResources;
using PlayerCamera;
using PlayerInputs.MoveIndicator;
using UI;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
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

        private const uint UNITS_GROUP = 1 << 1;

        private const uint BUILDINGS_GROUP = 1 << 2;

        private const uint RESOURCES_GROUP = 1 << 5;

        private const float DEFAULT_Z_POSITION = 100f;
        
        private const float DEFAULT_STOPPING_DISTANCE = 0.1f;
            
        private const float UNIT_STOPPING_DISTANCE = 1.5f;
            
        private const float RESOURCE_STOPPING_DISTANCE = 2.0f;


        private UnitTargetPositionComponent _unitTargetPositionComponent;

        private UnitSelectedTargetComponent _unitSelectedTargetComponent;

        private CheckGameplayInteractionPolicy _interactionPolicy;

        private MoveIndicatorController _moveIndicator;

        private CollisionFilter _selectionFilter;

        private CollisionFilter _targetSelectionFilter;

        private InputActions _inputActionMap;

        private bool _indicatorIsSet;

        private bool _isAvailable;

        private bool _anySelected;

        private bool _inputReceived;

        protected override void OnCreate()
        {
            _interactionPolicy = new CheckGameplayInteractionPolicy();
            _inputActionMap = new InputActions();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = RAYCAST_GROUP,
                CollidesWith = GROUNDPLANE_GROUP
            };

            _targetSelectionFilter = new CollisionFilter
            {
                BelongsTo = RAYCAST_GROUP,
                CollidesWith = UNITS_GROUP | BUILDINGS_GROUP | RESOURCES_GROUP
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
            _inputReceived = true;
        }

        private void CheckInteractionAvailable()
        {
            foreach (SetPlayerUIActionComponent playerUIActionComponent in SystemAPI.Query<SetPlayerUIActionComponent>())
            {
                if (playerUIActionComponent.Action != PlayerUIActionType.Build)
                {
                    continue;
                }

                _isAvailable = false;
            }
            _isAvailable = _interactionPolicy.IsAllowed();
        }

        private void SelectTargetPosition()
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            Camera mainCamera = EntityManager.GetComponentObject<MainCameraComponentData>(cameraEntity).Camera;
            RaycastInput targetInput = GetRaycastInput(mainCamera, _targetSelectionFilter);
            RaycastInput groundInput = GetRaycastInput(mainCamera, _selectionFilter);

            SetUnitPosition(collisionWorld, targetInput, groundInput);
        }

        private void SetUnitPosition(CollisionWorld collisionWorld, RaycastInput targetInput, RaycastInput groundInput)
        {
            bool hitTarget = collisionWorld.CastRay(targetInput, out var targetHit);
            Entity targetEntity = hitTarget ? targetHit.Entity : Entity.Null;

            if (!collisionWorld.CastRay(groundInput, out var groundHit))
            {
                return;
            }

            RaycastHit positionHit = hitTarget ? targetHit : groundHit;

            foreach ((RefRO<OwnerTagComponent> _, UnitTypeComponent unitType, Entity entity) in
                     SystemAPI.Query<RefRO<OwnerTagComponent>, UnitTypeComponent>().WithEntityAccess())
            {
                SetSelectedUnitPosition(positionHit, targetEntity, entity);
            }
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

            SetMovePositionIndicatorTransform();
        }

        private void SetMovePositionIndicatorTransform()
        {
            float3 spawnPosition = _unitTargetPositionComponent.Value;

            if (_unitSelectedTargetComponent.IsFollowingTarget)
            {
                Entity targetEntity = _unitSelectedTargetComponent.TargetEntity;
                LocalTransform targetTransform = EntityManager.GetComponentData<LocalTransform>(targetEntity);
                float3 position = targetTransform.Position;
                spawnPosition = position;
                spawnPosition.y = 0;

                float3 targetSize = CalculateTargetSize(targetEntity, targetTransform);
                _moveIndicator.SetTargetScale(targetSize);
            }
            else
            {
                _moveIndicator.SetDefaultScale();
            }

            _moveIndicator.Set(spawnPosition);
            _anySelected = false;
        }

        private float3 CalculateTargetSize(Entity targetEntity, LocalTransform targetTransform)
        {
            float3 size = new float3(1f, 1f, 1f); // Default size

            // Check if it's a building with BuildingObstacleSizeComponent
            if (EntityManager.HasComponent<BuildingObstacleSizeComponent>(targetEntity))
            {
                BuildingObstacleSizeComponent obstacleSize = EntityManager.GetComponentData<BuildingObstacleSizeComponent>(targetEntity);
                size = obstacleSize.Size;
            }

            // Apply the entity's scale
            size *= targetTransform.Scale;

            return size;
        }

        private float CalculateStoppingDistance(Entity targetEntity)
        {
            // Buildings: use half of their size as stopping distance
            if (EntityManager.HasComponent<BuildingObstacleSizeComponent>(targetEntity))
            {
                BuildingObstacleSizeComponent obstacleSize = EntityManager.GetComponentData<BuildingObstacleSizeComponent>(targetEntity);
                LocalTransform targetTransform = EntityManager.GetComponentData<LocalTransform>(targetEntity);

                // Calculate the maximum dimension (width or depth) and use half of it
                float maxDimension = math.max(obstacleSize.Size.x, obstacleSize.Size.z) * targetTransform.Scale;
                return maxDimension * 0.5f + 1.0f; // Half size + 1 unit buffer
            }

            // Resources: fixed stopping distance
            if (EntityManager.HasComponent<ResourceTypeComponent>(targetEntity))
            {
                return RESOURCE_STOPPING_DISTANCE;
            }

            // Units: fixed stopping distance
            if (EntityManager.HasComponent<UnitTagComponent>(targetEntity))
            {
                return UNIT_STOPPING_DISTANCE;
            }

            return DEFAULT_STOPPING_DISTANCE;
        }

        private void SetSelectedUnitPosition(RaycastHit closestHit, Entity targetEntity, Entity entity)
        {
            ElementSelectionComponent selectedPositionComponent = EntityManager.GetComponentData<ElementSelectionComponent>(entity);

            if (!selectedPositionComponent.IsSelected)
            {
                return;
            }

            _anySelected = true;

            _unitTargetPositionComponent = GetUnitPositionComponent(closestHit);
            EntityManager.SetComponentData(entity, _unitTargetPositionComponent);

            bool hasTarget = targetEntity != Entity.Null &&
                           EntityManager.Exists(targetEntity) &&
                           EntityManager.HasComponent<SelectableElementTypeComponent>(targetEntity);

            float stoppingDistance = 0.1f; // Default stopping distance
            if (hasTarget)
            {
                stoppingDistance = CalculateStoppingDistance(targetEntity);
            }

            _unitSelectedTargetComponent = new UnitSelectedTargetComponent
            {
                TargetEntity = hasTarget ? targetEntity : Entity.Null,
                IsFollowingTarget = hasTarget,
                StoppingDistance = stoppingDistance
            };
            EntityManager.SetComponentData(entity, _unitSelectedTargetComponent);

            PathComponent pathComp = EntityManager.GetComponentData<PathComponent>(entity);
            pathComp.HasPath = false;
            pathComp.CurrentWaypointIndex = 0;
            pathComp.LastTargetPosition = float3.zero;
            EntityManager.SetComponentData(entity, pathComp);

            SetMovePositionIndicator();
            
            DynamicBuffer<PathWaypointBuffer> pathBuffer = EntityManager.GetBuffer<PathWaypointBuffer>(entity);
            pathBuffer.Clear();
        }

        private UnitTargetPositionComponent GetUnitPositionComponent(RaycastHit closestHit)
        {
            return new UnitTargetPositionComponent
            {
                Value = closestHit.Position,
                MustMove =  true
            };
        }

        private RaycastInput GetRaycastInput(Camera mainCamera, CollisionFilter filter)
        {
            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = DEFAULT_Z_POSITION;
            Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mousePosition);

            return new RaycastInput
            {
                Start = mainCamera.transform.position,
                End = worldPosition,
                Filter = filter,
            };
        }

        protected override void OnUpdate()
        {
            if (!_inputReceived)
            {
                return;
            }

            _inputReceived = false;

            CheckInteractionAvailable();
            if (!_isAvailable)
            {
                return;
            }

            SelectTargetPosition();
        }
    }
}