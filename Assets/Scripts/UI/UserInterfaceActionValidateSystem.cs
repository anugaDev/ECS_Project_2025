using System.Collections.Generic;
using System.Linq;
using Buildings;
using GatherableResources;
using ScriptableObjects;
using Types;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace UI
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(UserInterfaceUpdateSelectionSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceActionValidateSystem : SystemBase
    {
        private Dictionary<BuildingType, BuildingScriptableObject> _buildingConfiguration;

        private Dictionary<UnitType, UnitScriptableObject> _unitsConfiguration;

        private ElementResourceCostPolicy _resourceCostPolicy;

        private EntityCommandBuffer _entityCommandBuffer;

        private Dictionary<PlayerUIActionType, System.Func<int, bool>> _actionValidators;

        private HashSet<UpdateUIActionPayload> _currentActions;

        protected override void OnCreate()
        {
            RequireForUpdate<PlayerTagComponent>();
            RequireForUpdate<BuildingsConfigurationComponent>();
            RequireForUpdate<UnitsConfigurationComponent>();
            _resourceCostPolicy = new ElementResourceCostPolicy();
            _currentActions = new HashSet<UpdateUIActionPayload>();
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            BuildingsScriptableObject buildingsConfig = SystemAPI.ManagedAPI.GetSingleton<BuildingsConfigurationComponent>().Configuration;
            _buildingConfiguration = buildingsConfig.GetBuildingsDictionary();

            UnitsScriptableObject unitsConfig = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>().Configuration;
            _unitsConfiguration = unitsConfig.GetUnitsDictionary();

            InitializeActionValidators();

            base.OnStartRunning();
        }

        private void InitializeActionValidators()
        {
            _actionValidators = new Dictionary<PlayerUIActionType, System.Func<int, bool>>
            {
                [PlayerUIActionType.Build] = (payloadID) => IsBuildingAffordable((BuildingType)payloadID),
                [PlayerUIActionType.Recruit] = (payloadID) => IsUnitAffordable((UnitType)payloadID),
                [PlayerUIActionType.Upgrade] = (payloadID) => true,
                [PlayerUIActionType.None] = (payloadID) => true
            };
        }

        protected override void OnUpdate()
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            
            StoreCurrentActions();
            CheckUpdateActionTags();
            
            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void StoreCurrentActions()
        {
            foreach ((RefRO<UpdateUIActionTag> updateUIActionTag, DynamicBuffer<UpdateUIActionPayload> buffer,
                         Entity entity) in
                     SystemAPI.Query<RefRO<UpdateUIActionTag>, DynamicBuffer<UpdateUIActionPayload>>()
                         .WithEntityAccess())
            {
                FillActionsHashSet(buffer);

                if (buffer.Length <= 0)
                {
                    return;
                }

                ValidatePlayerActions(entity);
            }
        }

        private void FillActionsHashSet(DynamicBuffer<UpdateUIActionPayload> buffer)
        {
            _currentActions = new HashSet<UpdateUIActionPayload>();
            for (int actionIndex = 0; actionIndex < buffer.Length; actionIndex++)
            {
                _currentActions.Add(buffer[actionIndex]);
            }
        }

        private void CheckUpdateActionTags()
        {
            foreach ((ValidateUIActionsTag tag, DynamicBuffer<UpdateUIActionPayload> buffer, Entity entity)
                     in SystemAPI.Query<ValidateUIActionsTag, DynamicBuffer<UpdateUIActionPayload>>().WithEntityAccess())
            {
                _entityCommandBuffer.RemoveComponent<ValidateUIActionsTag>(entity);
                FillActionsHashSet(buffer);
                ValidatePlayerActions(entity);
            }
        }

        private void ValidatePlayerActions(Entity entity)
        {
            UpdatePlayerResources(entity);
            ClearPreviousValidations(entity);
            ValidateActions(entity);
        }

        private void ClearPreviousValidations(Entity playerEntity)
        {
            DynamicBuffer<EnableUIActionBuffer> enableBuffer = EntityManager.GetBuffer<EnableUIActionBuffer>(playerEntity);
            DynamicBuffer<DisableUIActionBuffer> disableBuffer = EntityManager.GetBuffer<DisableUIActionBuffer>(playerEntity);

            enableBuffer.Clear();
            disableBuffer.Clear();
        }

        private void UpdatePlayerResources(Entity playerEntity)
        {
            int currentWood = SystemAPI.GetComponent<CurrentWoodComponent>(playerEntity).Value;
            int currentFood = SystemAPI.GetComponent<CurrentFoodComponent>(playerEntity).Value;
            CurrentPopulationComponent populationComponent = SystemAPI.GetComponent<CurrentPopulationComponent>(playerEntity);
            int currentPopulation = populationComponent.CurrentPopulation;
            int maxPopulation = populationComponent.MaxPopulation;
            
            _resourceCostPolicy.UpdateCost(currentWood, currentFood, currentPopulation, maxPopulation);
        }

        private void ValidateActions(Entity playerEntity)
        {
            if (_currentActions == null || _currentActions.Count == 0)
                return;

            foreach (UpdateUIActionPayload payload in _currentActions)
            {
                bool isAffordable = CheckActionAffordability(payload);

                if (isAffordable)
                {
                    AddEnableActionComponent(playerEntity, payload);
                }
                else
                {
                    AddDisableActionComponent(playerEntity, payload);
                }
            }
        }

        private bool CheckActionAffordability(UpdateUIActionPayload payload)
        {
            if (_actionValidators.TryGetValue(payload.Action, out var validator))
            {
                return validator(payload.PayloadID);
            }

            return true;
        }

        private bool IsBuildingAffordable(BuildingType buildingType)
        {
            if (!_buildingConfiguration.ContainsKey(buildingType))
            {
                return false;
            }
            
            List<ResourceCostEntity> costs = _buildingConfiguration[buildingType].ConstructionCost;
            return costs.All(cost => _resourceCostPolicy.Get(cost));
        }

        private bool IsUnitAffordable(UnitType unitType)
        {
            if (!_unitsConfiguration.ContainsKey(unitType))
            {
                return false;
            }
            
            List<ResourceCostEntity> costs = _unitsConfiguration[unitType].RecruitmentCost;
            return costs.All(cost => _resourceCostPolicy.Get(cost));
        }

        private void AddEnableActionComponent(Entity playerEntity, UpdateUIActionPayload payload)
        {
            DynamicBuffer<EnableUIActionBuffer> enableBuffer = EntityManager.GetBuffer<EnableUIActionBuffer>(playerEntity);
            enableBuffer.Add(new EnableUIActionBuffer
            {
                Action = payload.Action,
                PayloadID = payload.PayloadID
            });
        }

        private void AddDisableActionComponent(Entity playerEntity, UpdateUIActionPayload payload)
        {
            DynamicBuffer<DisableUIActionBuffer> disableBuffer = EntityManager.GetBuffer<DisableUIActionBuffer>(playerEntity);
            disableBuffer.Add(new DisableUIActionBuffer
            {
                Action = payload.Action,
                PayloadID = payload.PayloadID
            });
        }
    }
}