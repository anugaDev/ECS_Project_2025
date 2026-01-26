using System.Collections.Generic;
using System.Linq;
using Buildings;
using ElementCommons;
using GatherableResources;
using ScriptableObjects;
using Types;
using UI;
using UI.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class RecruitUnitsQueueSystem : SystemBase
    {
        private BuildingFactoryActionsFactory _buildingActionsFactory;

        private Dictionary<UnitType, UnitScriptableObject> _unitsConfiguration;
        
        private List<RecruitmentEntity> _recruitmentList;
        
        private List<RecruitmentEntity> _recuritmentQueue;

        private List<RecruitmentEntity> _endRecruitmentUnits;
        
        private EntityCommandBuffer _entityCommandBuffer;
        
        private ElementResourceCostPolicy _elementResourceCostPolicy;

        protected override void OnCreate()
        {
            RequireForUpdate<UnitsConfigurationComponent>();
            RequireForUpdate<PlayerTagComponent>();
            _buildingActionsFactory = new BuildingFactoryActionsFactory();
            _elementResourceCostPolicy = new ElementResourceCostPolicy();
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            _unitsConfiguration = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>().Configuration.GetUnitsDictionary();
            _recruitmentList = new List<RecruitmentEntity>();
            _recuritmentQueue = new List<RecruitmentEntity>();
            _endRecruitmentUnits = new List<RecruitmentEntity>();
            base.OnStartRunning();
        }

        protected override void OnUpdate()
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            CheckRecruitmentActions();
            UpdateUnitRecruitment();
            CheckRecruitmentQueue();
            RemoveEndedRecruitmentUnits();
            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void UpdatePolicy(Entity playerEntity)
        {
            int currentWood = SystemAPI.GetComponent<CurrentWoodComponent>(playerEntity).Value;
            int currentFood = SystemAPI.GetComponent<CurrentFoodComponent>(playerEntity).Value;
            CurrentPopulationComponent populationComponent = SystemAPI.GetComponent<CurrentPopulationComponent>(playerEntity);
            int currentPopulation = populationComponent.CurrentPopulation;
            int maxPopulation = populationComponent.MaxPopulation;
            _elementResourceCostPolicy.UpdateCost(currentWood, currentFood, currentPopulation, maxPopulation);
        }

        private void UpdateUnitRecruitment()
        {
            foreach (RecruitmentEntity recruitmentEntity in _recruitmentList)
            {
                UpdateRecruitment(recruitmentEntity);
            }
        }

        private void UpdateRecruitment(RecruitmentEntity recruitmentEntity)
        {
            recruitmentEntity.Update(SystemAPI.Time.DeltaTime);
            Entity buildingEntity = recruitmentEntity.Entity;
            float progress = recruitmentEntity.GetProgress();
            UnitType recruitmentEntityUnit = recruitmentEntity.Unit;
            EntityManager.SetComponentData(buildingEntity, new RecruitmentProgressComponent
            {
                UnitType = recruitmentEntityUnit,
                Value = progress
            });

            SetBuildingUpdateUI(buildingEntity);
        }

        private void RemoveEndedRecruitmentUnits()
        {
            foreach (RecruitmentEntity recruitmentEntity in _endRecruitmentUnits)
            {
                _recruitmentList.Remove(recruitmentEntity);
            }

            _endRecruitmentUnits.Clear();
        }

        private void CheckRecruitmentActions()
        {
            foreach ((SetPlayerUIActionComponent actionComponent, Entity entity) in SystemAPI.Query<SetPlayerUIActionComponent>().WithEntityAccess())
            {
                if (actionComponent.Action != PlayerUIActionType.Recruit)
                {
                    continue;
                }

                UpdatePolicy(entity);
                StartRecruitment(actionComponent);
                _entityCommandBuffer.RemoveComponent<SetPlayerUIActionComponent>(entity);
            }
        }

        private void StartRecruitment(SetPlayerUIActionComponent actionComponent)
        {
            UnitType unitType = (UnitType) actionComponent.PayloadID;
            if (!IsRecruitmentAvailable(unitType))
            {
                return;
            }

            SetUpdatedCosts(unitType);
            RecruitUnit(unitType);
        }

        private void SetUpdatedCosts(UnitType unitType)
        {
            _unitsConfiguration[unitType].RecruitmentCost
                .ForEach(_elementResourceCostPolicy.AddCost);
            Entity entity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            UpdateWoodResource(entity);
            UpdateFoodResource(entity);
            UpdatePopulation(entity);
            _entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(entity);
            _entityCommandBuffer.AddComponent<ValidateUIActionsTag>(entity);
        }

        private void UpdatePopulation(Entity entity)
        {
            CurrentPopulationComponent populationComponent = SystemAPI.GetComponent<CurrentPopulationComponent>(entity);
            SystemAPI.SetComponent(entity, new CurrentPopulationComponent
            {
                MaxPopulation = populationComponent.MaxPopulation,
                CurrentPopulation = _elementResourceCostPolicy.CurrentResources[ResourceType.Population]
            });
        }

        private void UpdateFoodResource(Entity entity)
        {
            SystemAPI.SetComponent(entity, new CurrentFoodComponent
            {
                Value = _elementResourceCostPolicy.CurrentResources[ResourceType.Food]
            });
        }

        private void UpdateWoodResource(Entity entity)
        {
            SystemAPI.SetComponent(entity, new CurrentWoodComponent
            {
                Value = _elementResourceCostPolicy.CurrentResources[ResourceType.Wood]
            });
        }

        private bool IsRecruitmentAvailable(UnitType unitType)
        {
            return _unitsConfiguration[unitType].RecruitmentCost.All(IsCostAffordable);
        }

        private bool IsCostAffordable(ResourceCostEntity costEntity)
        {
            return _elementResourceCostPolicy.Get(costEntity);
        }

        private void RecruitUnit(UnitType unitType)
        {
            foreach ((BuildingTypeComponent buildingTypeComponent, ElementSelectionComponent selectionComponent, Entity entity) in
                     SystemAPI.Query<BuildingTypeComponent, ElementSelectionComponent>().WithEntityAccess())
            {
                if (!selectionComponent.IsSelected)
                {
                    continue;
                }
                
                _buildingActionsFactory.Set(buildingTypeComponent.Type);

                if(_buildingActionsFactory.GetPayload(PlayerUIActionType.Build).Contains((int)unitType))
                {
                    RecruitUnitAtBuilding(unitType, entity);
                    return;
                }
            }
        }

        private void RecruitUnitAtBuilding(UnitType unitType, Entity entity)
        {
            float recruitmentTime = _unitsConfiguration[unitType].RecruitmentTime;
            RecruitmentEntity recruitmentEntity = new RecruitmentEntity(recruitmentTime, entity, unitType);
            recruitmentEntity.OnFinishedAction += OnUnitRecruitmentFinished;
            SetBuildingList(recruitmentEntity);
            SetBuildingBuffer(unitType, entity);
        }

        private void SetBuildingBuffer(UnitType unitType, Entity entity)
        {
            DynamicBuffer<RecruitmentQueueBufferComponent> recruitmentBuffer = GetRecruitmentBuffer(entity);
            recruitmentBuffer.Add(new RecruitmentQueueBufferComponent
            {
                unitType = unitType
            });

            SetBuildingUpdateUI(entity);
        }

        private void SetBuildingList(RecruitmentEntity recruitmentEntity)
        {
            if (_recruitmentList.Any(recruit => recruit.IsSameEntity(recruitmentEntity.Entity)))
            {
                _recuritmentQueue.Add(recruitmentEntity);
            }
            else
            { 
                _recruitmentList.Add(recruitmentEntity);
            }
        }

        private void OnUnitRecruitmentFinished(Entity building, UnitType unit, RecruitmentEntity recruitmentEntity)
        {
            SetRecruitmentEntityEnd(recruitmentEntity);
            LocalTransform buildingTransform = EntityManager.GetComponentData<LocalTransform>(building);

            SpawnUnitCommand buildingCommand = GetSpawnUnitCommand(buildingTransform.Position, unit);
            Entity entity = SystemAPI.GetSingletonEntity<PlayerTagComponent>();
            DynamicBuffer<SpawnUnitCommand> spawnUnitCommands = SystemAPI.GetBuffer<SpawnUnitCommand>(entity);
            spawnUnitCommands.AddCommandData(buildingCommand);
        }

        private void CheckRecruitmentQueue()
        {
            foreach (RecruitmentEntity endRecruitmentUnit in _endRecruitmentUnits)
            {
                CheckQueue(endRecruitmentUnit);
            }
        }
        private void CheckQueue(RecruitmentEntity doneEntity)
        {
            RecruitmentEntity queuedEntity = _recuritmentQueue.FirstOrDefault(recruit => recruit.IsSameEntity(doneEntity.Entity));

            if (queuedEntity == null)
            {
                return;
            }

            _recuritmentQueue.Remove(queuedEntity);
            _recruitmentList.Add(queuedEntity);
        }

        private void SetRecruitmentEntityEnd(RecruitmentEntity recruitmentEntity)
        {
            recruitmentEntity.OnFinishedAction -= OnUnitRecruitmentFinished;
            _endRecruitmentUnits.Add(recruitmentEntity);
            ClearRecruitmentBuffer(recruitmentEntity);
        }

        private void ClearRecruitmentBuffer(RecruitmentEntity recruitmentEntity)
        {
            Entity buildingEntity = recruitmentEntity.Entity;
            DynamicBuffer<RecruitmentQueueBufferComponent> recruitmentBuffer = GetRecruitmentBuffer(buildingEntity);
            recruitmentBuffer.RemoveAt(0);
            SetBuildingUpdateUI(buildingEntity);
        }

        private DynamicBuffer<RecruitmentQueueBufferComponent> GetRecruitmentBuffer(Entity entity)
        {
            return EntityManager.GetBuffer<RecruitmentQueueBufferComponent>(entity);
        }

        private SpawnUnitCommand GetSpawnUnitCommand(float3 buildingPosition, UnitType unit)
        {
            return new SpawnUnitCommand
            {
                Tick = SystemAPI.GetSingleton<NetworkTime>().ServerTick,
                UnitType = unit,
                BuildingPosition = buildingPosition
            };
        }

        public void SetBuildingUpdateUI(Entity entity)
        {
            ElementSelectionComponent elementSelectionComponent = EntityManager.GetComponentData<ElementSelectionComponent>(entity);
            elementSelectionComponent.MustUpdateGroup = true;
            EntityManager.SetComponentData(entity, elementSelectionComponent);
        }
    }
}