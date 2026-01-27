using System.Collections.Generic;
using System.Linq;
using Buildings;
using ElementCommons;
using GatherableResources;
using PlayerCamera;
using ScriptableObjects;
using Types;
using UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using BoxCollider = UnityEngine.BoxCollider;

namespace PlayerInputs
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class BuildingInputSystem : SystemBase
    {
        private const uint GROUNDPLANE_GROUP = 1 << 0;

        private const uint RAYCAST_GROUP = 1 << 5;

        private const float DEFAULT_Z_POSITION = 100f; 
        
        private Dictionary<BuildingType, BuildingScriptableObject> _buildingConfiguration;
        
        private BuildingMaterialsConfiguration _materialsConfiguration;

        private Dictionary<BuildingType, BuildingView> _buildingTemplates;
        
        private CheckGameplayInteractionPolicy _interactionPolicy;
        
        private BuildingView _currentBuildingTemplate;
        
        private BuildingType _currentBuildingType;
        
        private CollisionFilter _selectionFilter;
        
        private InputActions _inputActionMap;
        
        private bool _isBuilding;

        private bool _isPositionAvailable;

        private bool _lastAvailable;

        private Vector3 _lastPosition;

        private ElementResourceCostPolicy _elementResourceCostPolicy;

        private EntityCommandBuffer _entityCommandBuffer;

        protected override void OnCreate()
        {
            _interactionPolicy = new CheckGameplayInteractionPolicy();
            _buildingTemplates = new Dictionary<BuildingType, BuildingView>();
            _inputActionMap = new InputActions();
            RequireForUpdate<OwnerTagComponent>();
            RequireForUpdate<BuildingsConfigurationComponent>();
            _selectionFilter = new CollisionFilter
            {
                BelongsTo = RAYCAST_GROUP,
                CollidesWith = GROUNDPLANE_GROUP
            };
            _elementResourceCostPolicy = new ElementResourceCostPolicy();

            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            _inputActionMap.Enable();
            _inputActionMap.GameplayMap.SelectGameEntity.canceled += PlaceBuilding;
            _inputActionMap.GameplayMap.SelectMovePosition.performed += CancelBuilding;
            GetBuildingConfiguration();
            base.OnStartRunning();
        }

        protected override void OnStopRunning()
        {
            _inputActionMap.GameplayMap.SelectGameEntity.canceled -= PlaceBuilding;
            _inputActionMap.GameplayMap.SelectMovePosition.performed -= CancelBuilding;
            _inputActionMap.Disable();
            base.OnStopRunning();
        }

        private void GetBuildingConfiguration()
        {
            BuildingsScriptableObject configuration = SystemAPI.ManagedAPI.GetSingleton<BuildingsConfigurationComponent>().Configuration;
            _buildingConfiguration = configuration.GetBuildingsDictionary();
            BuildingMaterialsConfigurationComponent materialsComponent =
                SystemAPI.ManagedAPI.GetSingleton<BuildingMaterialsConfigurationComponent>();
            _materialsConfiguration = materialsComponent.Configuration;
        }

        protected override void OnUpdate()
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            if (_isBuilding)
            {
                UpdateBuilding();
            }
            else
            {
                CheckBuilding();
            }

            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void UpdateBuilding()
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            RaycastInput selectionInput = GetRaycastInput(collisionWorld);

            if (!collisionWorld.CastRay(selectionInput, out var closestHit))
            { 
                return;
            }

            _lastPosition = closestHit.Position;
            _currentBuildingTemplate.transform.position = _lastPosition;
            SetLastPositionAvailable();
        }

        private void SetLastPositionAvailable()
        {
            _isPositionAvailable = !CheckCollisionWithGhostElements();

            if (_isPositionAvailable != _lastAvailable)
            { 
                UpdateTemplateMaterial();
            }

            _lastAvailable = _isPositionAvailable;
        }

        private void UpdateTemplateMaterial()
        {
            if (_isPositionAvailable)
            {
                _currentBuildingTemplate.SetTeamColorMaterial(_materialsConfiguration.AvailableMaterial);
            }
            else
            {
                _currentBuildingTemplate.SetTeamColorMaterial(_materialsConfiguration.NotAvailableMaterial);
            }
        }

        private bool CheckCollisionWithGhostElements()
        {
            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
            CollisionWorld collisionWorld = physicsWorld.CollisionWorld;

            BoxCollider templateCollider = _currentBuildingTemplate.GameObject.GetComponent<BoxCollider>();
            Vector3 center = _lastPosition + templateCollider.center;
            Vector3 halfExtents = templateCollider.size * 0.5f;
            Quaternion rotation = Quaternion.identity;

            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u,
                GroupIndex = 0
            };

            collisionWorld.OverlapBox(center, rotation, halfExtents, ref hits, filter);

            int ghostCount = 0;
            for (int i = 0; i < hits.Length; i++)
            {
                Entity hitEntity = hits[i].Entity;
                if (EntityManager.HasComponent<SelectableElementTypeComponent>(hitEntity))
                {
                    ghostCount++;
                }
            }

            hits.Dispose();
            return ghostCount > 0;
        }

        private RaycastInput GetRaycastInput(CollisionWorld collisionWorld)
        {
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            Camera mainCamera = EntityManager.GetComponentObject<MainCameraComponentData>(cameraEntity).Camera;
            return GetRaycastInput(mainCamera);
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

        private void CheckBuilding()
        {
            foreach ((SetPlayerUIActionComponent playerUIActionComponent, Entity entity) in SystemAPI.Query<SetPlayerUIActionComponent>().WithEntityAccess())
            {
                if (playerUIActionComponent.Action != PlayerUIActionType.Build)
                {
                    continue;
                }

                CheckBuildingStatus(playerUIActionComponent, entity);
            }
        }

        private void CheckBuildingStatus(SetPlayerUIActionComponent playerUIActionComponent, Entity entity)
        {
            UpdateCosts(entity);
            if (!IsBuildingAvailable(playerUIActionComponent.PayloadID))
            {
                EndBuilding();
                return;
            }

            StartBuilding(playerUIActionComponent);
        }

        private void UpdateCosts(Entity playerEntity)
        {
            int currentWood = SystemAPI.GetComponent<CurrentWoodComponent>(playerEntity).Value;
            int currentFood = SystemAPI.GetComponent<CurrentFoodComponent>(playerEntity).Value;
            CurrentPopulationComponent populationComponent = SystemAPI.GetComponent<CurrentPopulationComponent>(playerEntity);
            int currentPopulation = populationComponent.CurrentPopulation;
            int maxPopulation = populationComponent.MaxPopulation;
            _elementResourceCostPolicy.UpdateCost(currentWood, currentFood, currentPopulation, maxPopulation);
        }

        private bool IsBuildingAvailable(int payloadID)
        {
            return _buildingConfiguration[(BuildingType)payloadID].ConstructionCost.All(IsCostAffordable);
        }

        private bool IsCostAffordable(ResourceCostEntity cost)
        {
            return _elementResourceCostPolicy.Get(cost);
        }

        private void StartBuilding(SetPlayerUIActionComponent playerUIActionComponent)
        {
            _isBuilding = true;
            _currentBuildingType = (BuildingType)playerUIActionComponent.PayloadID;
            SetCurrentTemplate();
            _currentBuildingTemplate.GameObject.SetActive(true);
        }

        private void SetCurrentTemplate()
        {
            if (!_buildingTemplates.ContainsKey(_currentBuildingType))
            {
                InstantiateBuildingType();
            }

            _currentBuildingTemplate = _buildingTemplates[_currentBuildingType];
        }

        private void InstantiateBuildingType()
        {
            BuildingView buildingView =
                Object.Instantiate(_buildingConfiguration[_currentBuildingType].BuildingTemplate);
            _buildingTemplates.Add(_currentBuildingType, buildingView);
        }

        private void CancelBuilding(InputAction.CallbackContext _)
        {
            if (!_isBuilding)
            {
                return;
            }

            EndBuilding();
        }

        private void PlaceBuilding(InputAction.CallbackContext _)
        {
            if (!IsBuildingPlacingAvailable())
            {
                return;
            }

            SetUpdatedCosts();
            SetBuildingComponent();
            EndBuilding();
        }

        private void SetUpdatedCosts()
        {
            // NOTE: Resource deduction is now handled on the server in PlaceBuildingCommandServerSystem
            // We only update the local policy for UI validation purposes
            _buildingConfiguration[_currentBuildingType].ConstructionCost
                .ForEach(_elementResourceCostPolicy.AddCost);

            // The server will update the actual resource components and trigger UpdateResourcesPanelTag
            // No need to modify components here - they are GhostFields and server is authoritative
        }

        private bool IsBuildingPlacingAvailable()
        {
            return _isBuilding && _interactionPolicy.IsAllowed() && _isPositionAvailable;
        }

        private void SetBuildingComponent()
        {
            PlaceBuildingCommand buildingCommand = GetBuildingComponent();
            Entity entity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            DynamicBuffer<PlaceBuildingCommand> placeBuildingCommands = SystemAPI.GetBuffer<PlaceBuildingCommand>(entity);
            placeBuildingCommands.AddCommandData(buildingCommand);
        }

        private PlaceBuildingCommand GetBuildingComponent()
        {
            return new PlaceBuildingCommand
            {
                Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick,
                BuildingType = _currentBuildingType,
                Position = _lastPosition
            };
        }

        private void EndBuilding()
        {
            _isBuilding = false;
            _currentBuildingTemplate.GameObject.SetActive(false);

            Entity entity = SystemAPI.GetSingletonEntity<SetPlayerUIActionComponent>();

            if (_entityCommandBuffer.IsCreated)
            {
                _entityCommandBuffer.RemoveComponent<SetPlayerUIActionComponent>(entity);
            }
            else
            {
                EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
                ecb.RemoveComponent<SetPlayerUIActionComponent>(entity);
                ecb.Playback(EntityManager);
                ecb.Dispose();
            }
        }
    }
}