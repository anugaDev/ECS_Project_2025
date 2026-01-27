using Buildings;
using ElementCommons;
using GatherableResources;
using PlayerInputs;
using Types;
using Units.Worker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using CurrentResourceQuantityComponent = GatherableResources.CurrentResourceQuantityComponent;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMoveSystem))]
    public partial struct UnitTargetActionTriggerSystem : ISystem
    {
        private ComponentLookup<ElementTeamComponent> _teamLookup;

        private ComponentLookup<ResourceTypeComponent> _resourceLookup;

        private ComponentLookup<BuildingComponents> _buildingLookup;

        private ComponentLookup<UnitTagComponent> _unitLookup;

        private ComponentLookup<BuildingTypeComponent> _buildingTypeLookup;

        private ComponentLookup<CurrentResourceQuantityComponent> _currentResourceQuantityLookup;
        
        private ComponentLookup<BuildingConstructionProgressComponent> _constructionProgressLookup;

        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _teamLookup = state.GetComponentLookup<ElementTeamComponent>(true);
            _resourceLookup = state.GetComponentLookup<ResourceTypeComponent>(true);
            _buildingLookup = state.GetComponentLookup<BuildingComponents>(true);
            _unitLookup = state.GetComponentLookup<UnitTagComponent>(true);
            _buildingTypeLookup = state.GetComponentLookup<BuildingTypeComponent>(true);
            _currentResourceQuantityLookup = state.GetComponentLookup<CurrentResourceQuantityComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _teamLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _buildingLookup.Update(ref state);
            _unitLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _currentResourceQuantityLookup.Update(ref state);

            foreach ((RefRO<UnitTargetPositionComponent> targetPosition, RefRW<UnitSelectedTargetComponent> selectedTarget,
                     RefRO<UnitTypeComponent> unitType, RefRO<ElementTeamComponent> unitTeam,
                     RefRW<UnitTargetEntity> attackTarget, Entity unitEntity)
                     in SystemAPI.Query<RefRO<UnitTargetPositionComponent>, RefRW<UnitSelectedTargetComponent>,
                             RefRO<UnitTypeComponent>, RefRO<ElementTeamComponent>, RefRW<UnitTargetEntity>>()
                         .WithAll<Simulate, UnitTagComponent>() .WithEntityAccess())
            {
                if (targetPosition.ValueRO.MustMove || !selectedTarget.ValueRO.IsFollowingTarget)
                {
                    continue;
                }

                Entity targetEntity = selectedTarget.ValueRO.TargetEntity;

                if (targetEntity == Entity.Null || !state.EntityManager.Exists(targetEntity))
                {
                    selectedTarget.ValueRW.IsFollowingTarget = false;
                    selectedTarget.ValueRW.TargetEntity = Entity.Null;
                    continue;
                }

                TriggerActionForCombatUnit(ref state, unitEntity, targetEntity, unitType.ValueRO.Type,
                            unitTeam.ValueRO.Team, ref selectedTarget.ValueRW, ref attackTarget.ValueRW);
            }

            foreach ((RefRO<UnitTargetPositionComponent> targetPosition, RefRW<UnitSelectedTargetComponent> selectedTarget,
                     UnitTypeComponent unitType, RefRO<ElementTeamComponent> unitTeam, Entity unitEntity)
                     in SystemAPI.Query<RefRO<UnitTargetPositionComponent>, RefRW<UnitSelectedTargetComponent>,
                             UnitTypeComponent, RefRO<ElementTeamComponent>>()
                         .WithAll<Simulate, UnitTagComponent>().WithNone<UnitTargetEntity>().WithEntityAccess())
            {
                if (targetPosition.ValueRO.MustMove || !selectedTarget.ValueRO.IsFollowingTarget || unitType.Type != UnitType.Worker)
                {
                    continue;
                }

                Entity targetEntity = selectedTarget.ValueRO.TargetEntity;

                if (targetEntity == Entity.Null || !state.EntityManager.Exists(targetEntity))
                {
                    selectedTarget.ValueRW.IsFollowingTarget = false;
                    selectedTarget.ValueRW.TargetEntity = Entity.Null;
                    continue;
                }

                TriggerActionForWorker(ref state, unitEntity, targetEntity, unitTeam.ValueRO.Team, ref selectedTarget.ValueRW);
            }
            
            _entityCommandBuffer.Playback(state.EntityManager);
        }

        [BurstCompile]
        private void TriggerActionForCombatUnit(ref SystemState state, Entity unitEntity, Entity targetEntity,
                                   UnitType unitType, TeamType unitTeam,
                                   ref UnitSelectedTargetComponent selectedTarget,
                                   ref UnitTargetEntity attackTarget)
        {
            bool isEnemy = false;
            if (_teamLookup.HasComponent(targetEntity))
            {
                TeamType targetTeam = _teamLookup[targetEntity].Team;
                isEnemy = targetTeam != unitTeam;
            }

            if (isEnemy && (_unitLookup.HasComponent(targetEntity) || _buildingLookup.HasComponent(targetEntity)))
            {
                attackTarget.Value = targetEntity;
                return;
            }

            SetUnitTargetOff(unitEntity, selectedTarget);
        }

        [BurstCompile]
        private void TriggerActionForWorker(ref SystemState state, Entity unitEntity, Entity targetEntity,
                                           TeamType unitTeam, ref UnitSelectedTargetComponent selectedTarget)
        {
            if (_resourceLookup.HasComponent(targetEntity))
            {
                SetWorkerGatheringResource(selectedTarget, unitEntity);
            }

            if (_buildingLookup.HasComponent(targetEntity))
            {
                SetWorkerProcess(unitEntity, targetEntity, selectedTarget, unitTeam);
            }

            SetUnitTargetOff(unitEntity, selectedTarget);
        }

        private void SetUnitTargetOff(Entity unitEntity, UnitSelectedTargetComponent selectedTarget)
        {
            selectedTarget.IsFollowingTarget = false;
            selectedTarget.TargetEntity = Entity.Null;
            _entityCommandBuffer.AddComponent(unitEntity, selectedTarget);
        }

        private void SetWorkerProcess(Entity unitEntity, Entity targetEntity, UnitSelectedTargetComponent selectedTarget,
            TeamType unitTeam)
        {
            TeamType buildingTeam = _teamLookup[targetEntity].Team;
            if (buildingTeam != unitTeam)
            {
                return;
            }

            if (_constructionProgressLookup.HasComponent(targetEntity))
            { 
                SetWorkerBuildingMode(selectedTarget, unitEntity);
                return;
            }
            
            if (IsWorkerStoreResourcesAvailable(targetEntity, unitEntity))
            { 
                SetWorkerStoreResources(unitEntity, targetEntity);
            }            
            
        }

        private void SetWorkerStoreResources(Entity unitEntity, Entity targetEntity)
        {
            
            _entityCommandBuffer.AddComponent(unitEntity, GetStoringComponent(targetEntity));
        }

        private WorkerStoringTagComponent GetStoringComponent(Entity targetEntity)
        {
            return new WorkerStoringTagComponent
            {
                BuildingEntity = targetEntity
            };
        }

        private bool IsWorkerStoreResourcesAvailable(Entity targetEntity, Entity unitEntity)
        {
            CurrentResourceQuantityComponent resourceQuantity = _currentResourceQuantityLookup[unitEntity];
            BuildingTypeComponent buildingType = _buildingTypeLookup[targetEntity];
            return resourceQuantity.Value > 0 && buildingType.Type == BuildingType.Center;
        }

        private void SetWorkerGatheringResource(UnitSelectedTargetComponent selectedTarget, Entity unitEntity)
        {
            _entityCommandBuffer.AddComponent(unitEntity, GetGatheringComponent(selectedTarget.TargetEntity));
        }

        private WorkerGatheringTagComponent GetGatheringComponent(Entity selectedTargetTargetEntity)
        {
            return new WorkerGatheringTagComponent
            {
                ResourceEntity = selectedTargetTargetEntity
            };
        }

        private void SetWorkerBuildingMode(UnitSelectedTargetComponent selectedTarget, Entity unitEntity)
        {
            _entityCommandBuffer.AddComponent(unitEntity, GetBuildingComponent(selectedTarget.TargetEntity));
        }

        private WorkerConstructionTagComponent GetBuildingComponent(Entity selectedTargetTargetEntity)
        {
            return new WorkerConstructionTagComponent
            {
                BuildingEntity = selectedTargetTargetEntity
            };
        }
    }
}

