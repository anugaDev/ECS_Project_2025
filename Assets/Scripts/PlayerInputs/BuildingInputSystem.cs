using System.Collections.Generic;
using Buildings;
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

        private Dictionary<BuildingType, BuildingView> _buildingTemplates;
        
        private CheckGameplayInteractionPolicy _interactionPolicy;
        
        private BuildingView _currentBuildingTemplate;
        
        private BuildingType _currentBuildingType;
        
        private CollisionFilter _selectionFilter;
        
        private InputActions _inputActionMap;
        
        private bool _isBuilding;

        private bool _isPositionAvailable;
        
        private Vector3 _lastPosition;

        private EntityQuery _pendingNetworkIdQuery;

        protected override void OnCreate()
        {
            EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<NetworkId>().WithNone<NetworkStreamInGame>();
            _pendingNetworkIdQuery = GetEntityQuery(builder);
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
        }

        protected override void OnUpdate()
        {
            if (_isBuilding)
            {
                UpdateBuilding();
                return;
            }

            CheckBuilding();
        }

        private void UpdateBuilding()
        {
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            var selectionInput = GetRaycastInput(collisionWorld);

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
            _isPositionAvailable = true; //TODO Check Template collision
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
            foreach ((SetPlayerUIActionComponent playerUIActionComponent, PlayerTagComponent playerTag) in SystemAPI.Query<SetPlayerUIActionComponent, PlayerTagComponent>())
            {
                if (playerUIActionComponent.Action != PlayerUIActionType.Build)
                {
                    continue;
                }

                CheckBuildingStatus(playerUIActionComponent);
            }
        }

        private void CheckBuildingStatus(SetPlayerUIActionComponent playerUIActionComponent)
        {
            if (!IsBuildingAvailable(playerUIActionComponent.PayloadID))
            {
                EndBuilding();
                return;
            }

            StartBuilding(playerUIActionComponent);
        }

        private bool IsBuildingAvailable(int payloadID)
        {
            return true; //TODO CHECK IF RESOURCES AVAILABLE
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

            SetBuildingComponent();
            EndBuilding();
        }

        private bool IsBuildingPlacingAvailable()
        {
            return _isBuilding && _interactionPolicy.IsAllowed() && _isPositionAvailable;
        }

        private void SetBuildingComponent()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            PlaceBuildingCommand buildingCommand = GetBuildingComponent();
            Entity entity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            DynamicBuffer<PlaceBuildingCommand> placeBuildingCommands = SystemAPI.GetBuffer<PlaceBuildingCommand>(entity);
            placeBuildingCommands.AddCommandData(buildingCommand);
            entityCommandBuffer.Playback(EntityManager);
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
            EntityManager.RemoveComponent<SetPlayerUIActionComponent>(entity);
        }
    }
}