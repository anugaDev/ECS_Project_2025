using System.Collections.Generic;
using System.Linq;
using Buildings;
using ElementCommons;
using ScriptableObjects;
using Types;
using UI;
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

        protected override void OnCreate()
        {
            RequireForUpdate<UnitsConfigurationComponent>();
            RequireForUpdate<PlayerTagComponent>();
            _buildingActionsFactory = new BuildingFactoryActionsFactory();
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
            CheckRecruitmentActions();
            UpdateUnitRecruitment();
            RemoveEndedRecruitmentUnits();
            CheckRecruitmentQueue();
        }

        private void UpdateUnitRecruitment()
        {
            foreach (RecruitmentEntity recruitmentEntity in _recruitmentList)
            {
                recruitmentEntity.Update(SystemAPI.Time.DeltaTime);
            }
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
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((SetPlayerUIActionComponent actionComponent, Entity entity) in SystemAPI.Query<SetPlayerUIActionComponent>().WithEntityAccess())
            {
                if (actionComponent.Action != PlayerUIActionType.Recruit)
                {
                    continue;
                }

                StartRecruitment(actionComponent);
                entityCommandBuffer.RemoveComponent<SetPlayerUIActionComponent>(entity);
            }

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
        }

        private void StartRecruitment(SetPlayerUIActionComponent actionComponent)
        {
            if (!IsRecruitmentAvailable())
            {
                return;
            }

            RecruitUnit((UnitType) actionComponent.PayloadID);
        }

        private bool IsRecruitmentAvailable()
        {
            return true; //TODO SET Recruitment Costs
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
    }
}