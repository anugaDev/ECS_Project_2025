using System.Collections.Generic;
using Buildings;
using ScriptableObjects;
using Types;
using UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlayerInputs
{
    public partial class PlayerInputBuildingSystem : SystemBase
    {
        private bool _isBuilding;

        private BuildingView _currentBuildingTemplate;
        
        private InputActions _inputActionMap;

        private BuildingType _currentBuildingType;

        private Dictionary<BuildingType, BuildingScriptableObject> _buildingConfiguration;

        private Dictionary<BuildingType, BuildingView> _buildingTemplates;

        protected override void OnCreate()
        {
            _buildingTemplates = new Dictionary<BuildingType, BuildingView>();
            _inputActionMap = new InputActions();
            RequireForUpdate<BuildingsConfigurationComponent>();
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
            Vector2 mousePosition = _inputActionMap.GameplayMap.PointerPosition.ReadValue<Vector2>();
            _currentBuildingTemplate.transform.position =
                new Vector3(mousePosition.x, GlobalParameters.DEFAULT_SCENE_HEIGHT, mousePosition.y);
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