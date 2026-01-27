using Buildings;
using Combat;
using ElementCommons;
using GatherableResources;
using PlayerInputs;
using Types;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

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
        
        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _teamLookup = state.GetComponentLookup<ElementTeamComponent>(true);
            _resourceLookup = state.GetComponentLookup<ResourceTypeComponent>(true);
            _buildingLookup = state.GetComponentLookup<BuildingComponents>(true);
            _unitLookup = state.GetComponentLookup<UnitTagComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _teamLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _buildingLookup.Update(ref state);
            _unitLookup.Update(ref state);

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
                return;
            }

            if (_buildingLookup.HasComponent(targetEntity))
            {
                SetWorkerInBuildingProcess(unitEntity, selectedTarget, unitTeam);
                return;
            }

            SetUnitTargetOff(unitEntity, selectedTarget);
        }

        private void SetUnitTargetOff(Entity unitEntity, UnitSelectedTargetComponent selectedTarget)
        {
            selectedTarget.IsFollowingTarget = false;
            selectedTarget.TargetEntity = Entity.Null;
            _entityCommandBuffer.SetComponent(unitEntity, selectedTarget);
        }

        private void SetWorkerInBuildingProcess(Entity unitEntity, UnitSelectedTargetComponent selectedTarget,
            TeamType unitTeam)
        {
            //if()TODO Building is in progress and same team
            SetWorkerBuildingMode(selectedTarget, unitEntity);
            //else TODO Worker is carrying and building is center and same team
            SetWorkerStoreResources();
        }

        private void SetWorkerStoreResources()
        {
            //TODO Implement resource storing
        }

        private void SetWorkerGatheringResource(UnitSelectedTargetComponent selectedTarget, Entity unitEntity)
        {
            // TODO: Implement resource gathering
        }

        private void SetWorkerBuildingMode(UnitSelectedTargetComponent selectedTarget, Entity unitEntity)
        {
            // TODO: Check if building is under construction
            // TODO: Implement building construction
        }
    }
}

