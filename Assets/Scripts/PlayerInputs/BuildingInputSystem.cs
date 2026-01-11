using System.Collections.Generic;
using Buildings;
using PlayerCamera;
using ScriptableObjects;
using Types;
using UI;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerInputs
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    public partial class BuildingInputSystem : SystemBase
    {
        private const uint GROUNDPLANE_GROUP = 1 << 0; 
        
        private const uint RAYCAST_GROUP = 1 << 5; 

        private const float DEFAULT_Z_POSITION = 100f; 
        
        private bool _isBuilding;

        private BuildingView _currentBuildingTemplate;
        
        private InputActions _inputActionMap;

        private BuildingType _currentBuildingType;

        private Dictionary<BuildingType, BuildingScriptableObject> _buildingConfiguration;

        private Dictionary<BuildingType, BuildingView> _buildingTemplates;
        
        private CollisionFilter _selectionFilter;

        protected override void OnCreate()
        {
            _buildingTemplates = new Dictionary<BuildingType, BuildingView>();
            _inputActionMap = new InputActions();
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
            _inputActionMap.GameplayMap.SelectGameEntity.started += PlaceBuilding;
            _inputActionMap.GameplayMap.SelectMovePosition.started += CancelBuilding;
            GetBuildingConfiguration();
            base.OnStartRunning();
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

            _currentBuildingTemplate.transform.position = closestHit.Position;
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
            foreach (SetPlayerUIActionComponent playerUIActionComponent in SystemAPI.Query<SetPlayerUIActionComponent>())
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
            if (!_isBuilding)
            {
                return;
            }

            EndBuilding();
        }

        private void EndBuilding()
        {
            _isBuilding = false;
            _currentBuildingTemplate.GameObject.SetActive(false);
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer();
            Entity entity = SystemAPI.GetSingletonEntity<SetPlayerUIActionComponent>();
            entityCommandBuffer.RemoveComponent<SetPlayerUIActionComponent>(entity);
            entityCommandBuffer.Playback(EntityManager);
        }
    }
}