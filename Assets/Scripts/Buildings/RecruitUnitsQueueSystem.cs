using System.Collections.Generic;
using System.Linq;
using ElementCommons;
using ScriptableObjects;
using Types;
using UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Buildings
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial class RecruitUnitsQueueSystem : SystemBase
    {
        private BuildingFactoryActionsFactory _buildingActionsFactory;

        private Dictionary<UnitType, UnitScriptableObject> _unitsConfiguration;
        
        private Dictionary<UnitType, Entity> _prefabConfiguration;

        private List<RecruitmentEntity> _recruitmentList;

        protected override void OnCreate()
        {
            _buildingActionsFactory = new BuildingFactoryActionsFactory();
            base.OnCreate();
        }

        protected override void OnStartRunning()
        {
            _unitsConfiguration = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>().Configuration.GetUnitsDictionary();
            _recruitmentList = new List<RecruitmentEntity>();
            FillPrefabDictionary();
            base.OnStartRunning();
        }

        private void FillPrefabDictionary()
        {
            Entity singletonEntity = SystemAPI.GetSingletonEntity<UnitPrefabComponent>();
            UnitPrefabComponent unitPrefabComponent = SystemAPI.GetComponent<UnitPrefabComponent>(singletonEntity);
            _prefabConfiguration = new Dictionary<UnitType, Entity>
            {
                {UnitType.Ballista, unitPrefabComponent.Ballista},
                {UnitType.Worker, unitPrefabComponent.Worker},
                {UnitType.Warrior, unitPrefabComponent.Warrior},
                {UnitType.Archer, unitPrefabComponent.Archer}
            };
        }

        protected override void OnUpdate()
        {
            CheckRecruitmentActions();
            UpdateUnitRecruitment();
        }

        private void UpdateUnitRecruitment()
        {
            foreach (RecruitmentEntity recruitmentEntity in _recruitmentList)
            {
                recruitmentEntity.Update(SystemAPI.Time.DeltaTime);
            }
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
            RecruitmentEntity recruitmentEntity = new RecruitmentEntity(recruitmentTime, entity);
            recruitmentEntity.OnFinishedAction += OnUnitRecruitmentFinished;
            _recruitmentList.Add(recruitmentEntity);
        }

        private void OnUnitRecruitmentFinished(Entity building, UnitType unit)
        {
            //EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            //Entity newBuilding = entityCommandBuffer.Instantiate(_prefabConfiguration[unit]);
            //LocalTransform buildingTransform = SystemAPI.GetComponent<LocalTransform>(building);
            //entityCommandBuffer.SetComponent(newBuilding, LocalTransform.FromPosition(buildingTransform.Position));
            //entityCommandBuffer.SetComponent(newBuilding, new GhostOwner{NetworkId = networkId});
            //entityCommandBuffer.SetComponent(newBuilding, new ElementTeamComponent{Team = playerTeam});
        }
    }
}