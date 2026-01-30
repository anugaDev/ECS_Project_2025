using Buildings;
using ElementCommons;
using GatherableResources;
using PlayerInputs;
using Types;
using Units.Worker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UnitMoveSystem))]
    [UpdateAfter(typeof(Worker.WorkerGatheringSystem))]
    [UpdateAfter(typeof(Worker.WorkerStoringSystem))]
    public partial struct UnitTargetActionTriggerSystem : ISystem
    {
        private ComponentLookup<ElementTeamComponent> _teamLookup;

        private ComponentLookup<ResourceTypeComponent> _resourceLookup;

        private ComponentLookup<BuildingComponents> _buildingLookup;

        private ComponentLookup<UnitTagComponent> _unitLookup;

        private ComponentLookup<BuildingTypeComponent> _buildingTypeLookup;

        private ComponentLookup<CurrentWorkerResourceQuantityComponent> _currentResourceQuantityLookup;
        
        private ComponentLookup<BuildingConstructionProgressComponent> _constructionProgressLookup;

        private ComponentLookup<WorkerGatheringTagComponent> _gatheringTagLookup;

        private ComponentLookup<WorkerStoringTagComponent> _storingTagLookup;

        private ComponentLookup<WorkerConstructionTagComponent> _constructionTagLookup;

        private EntityCommandBuffer _entityCommandBuffer;

        private NativeHashSet<Entity> _entitiesWithPendingComponents;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            InitializeComponentLookup(state);
            _entitiesWithPendingComponents = new NativeHashSet<Entity>(64, Allocator.Persistent);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_entitiesWithPendingComponents.IsCreated)
            {
                _entitiesWithPendingComponents.Dispose();
            }
        }

        private void InitializeComponentLookup(SystemState state)
        {
            _teamLookup = state.GetComponentLookup<ElementTeamComponent>(true);
            _resourceLookup = state.GetComponentLookup<ResourceTypeComponent>(true);
            _buildingLookup = state.GetComponentLookup<BuildingComponents>(true);
            _unitLookup = state.GetComponentLookup<UnitTagComponent>(true);
            _buildingTypeLookup = state.GetComponentLookup<BuildingTypeComponent>(true);
            _currentResourceQuantityLookup = state.GetComponentLookup<CurrentWorkerResourceQuantityComponent>(true);
            _constructionProgressLookup = state.GetComponentLookup<BuildingConstructionProgressComponent>(true);
            _gatheringTagLookup = state.GetComponentLookup<WorkerGatheringTagComponent>(true);
            _storingTagLookup = state.GetComponentLookup<WorkerStoringTagComponent>(true);
            _constructionTagLookup = state.GetComponentLookup<WorkerConstructionTagComponent>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Only server should trigger worker actions to avoid desync
            if (!state.WorldUnmanaged.IsServer())
            {
                return;
            }

            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick)
            {
                return;
            }
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            _teamLookup.Update(ref state);
            _resourceLookup.Update(ref state);
            _buildingLookup.Update(ref state);
            _unitLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _currentResourceQuantityLookup.Update(ref state);
            _constructionProgressLookup.Update(ref state);
            _gatheringTagLookup.Update(ref state);
            _storingTagLookup.Update(ref state);
            _constructionTagLookup.Update(ref state);

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
                     UnitTypeComponent unitType, RefRO<ElementTeamComponent> unitTeam, RefRO<LocalTransform> unitTransform,
                     Entity unitEntity)
                     in SystemAPI.Query<RefRO<UnitTargetPositionComponent>, RefRW<UnitSelectedTargetComponent>,
                             UnitTypeComponent, RefRO<ElementTeamComponent>, RefRO<LocalTransform>>()
                         .WithAll<Simulate, UnitTagComponent>()
                         .WithNone<UnitTargetEntity, WorkerGatheringTagComponent>()
                         .WithNone<WorkerStoringTagComponent, WorkerConstructionTagComponent>()
                         .WithEntityAccess())
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

                if (state.WorldUnmanaged.IsServer())
                {
                    UnityEngine.Debug.Log($"[QUERY] Processing worker {unitEntity.Index}:{unitEntity.Version} from worker-specific query (Frame: {UnityEngine.Time.frameCount})");
                    UnityEngine.Debug.Log($"[QUERY] Worker {unitEntity.Index} current target: {targetEntity.Index}, IsFollowing: {selectedTarget.ValueRO.IsFollowingTarget}");
                }
                TriggerActionForWorker(ref state, unitEntity, targetEntity, unitTeam.ValueRO.Team, ref selectedTarget.ValueRW, unitTransform.ValueRO);
            }
            
            _entityCommandBuffer.Playback(state.EntityManager);

            // Clear the pending set - components have been applied, workers can trigger new actions next frame
            _entitiesWithPendingComponents.Clear();
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
        }

        [BurstCompile]
        private void TriggerActionForWorker(ref SystemState state, Entity unitEntity, Entity targetEntity,
                                           TeamType unitTeam, ref UnitSelectedTargetComponent selectedTarget, LocalTransform unitTransform)
        {
            // IMPORTANT: Only trigger actions when worker is close to AND following the same target
            // This prevents re-triggering when worker is sent to a different target (e.g., Town Center after gathering)
            if (!state.EntityManager.HasComponent<LocalTransform>(targetEntity))
            {
                return;
            }

            LocalTransform targetTransform = state.EntityManager.GetComponentData<LocalTransform>(targetEntity);
            float distanceSq = math.distancesq(unitTransform.Position, targetTransform.Position);
            const float TRIGGER_DISTANCE_SQ = 4.0f * 4.0f; // 4 units

            if (distanceSq > TRIGGER_DISTANCE_SQ)
            {
                if (state.WorldUnmanaged.IsServer())
                {
                    UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} too far from target {targetEntity.Index} (distance: {math.sqrt(distanceSq):F2}), skipping");
                }
                return;
            }

            // Check if worker is actually following this target (not a different one)
            // This prevents triggering when worker is still at resource location but following Town Center
            if (selectedTarget.TargetEntity != targetEntity)
            {
                if (state.WorldUnmanaged.IsServer())
                {
                    UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} is close to {targetEntity.Index} but following different target {selectedTarget.TargetEntity.Index}, skipping");
                }
                return;
            }

            if (state.WorldUnmanaged.IsServer())
            {
                bool hasGathering = _gatheringTagLookup.HasComponent(unitEntity);
                bool hasStoring = _storingTagLookup.HasComponent(unitEntity);
                bool hasConstruction = _constructionTagLookup.HasComponent(unitEntity);
                bool hasPending = _entitiesWithPendingComponents.Contains(unitEntity);
                bool targetIsResource = _resourceLookup.HasComponent(targetEntity);
                bool targetIsBuilding = _buildingLookup.HasComponent(targetEntity);
                UnityEngine.Debug.Log($"[TRIGGER-CHECK] Worker {unitEntity.Index} targeting {targetEntity.Index} (distance: {math.sqrt(distanceSq):F2}, Resource:{targetIsResource}, Building:{targetIsBuilding}) - Gathering:{hasGathering}, Storing:{hasStoring}, Construction:{hasConstruction}, Pending:{hasPending}");
            }

            // Check if worker already has any action tag - prevents re-triggering
            if (_gatheringTagLookup.HasComponent(unitEntity) ||
                _storingTagLookup.HasComponent(unitEntity) ||
                _constructionTagLookup.HasComponent(unitEntity))
            {
                if (state.WorldUnmanaged.IsServer())
                {
                    UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index}:{unitEntity.Version} already has action tag, skipping");
                }
                return;
            }

            // Check if we've already queued a component for this entity (but it hasn't been applied yet)
            if (_entitiesWithPendingComponents.Contains(unitEntity))
            {
                if (state.WorldUnmanaged.IsServer())
                {
                    UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index}:{unitEntity.Version} has pending component, skipping (Frame: {UnityEngine.Time.frameCount})");
                }
                return;
            }

            if (_resourceLookup.HasComponent(targetEntity))
            {
                // Don't add gathering component if worker already has it (prevents re-triggering)
                if (_gatheringTagLookup.HasComponent(unitEntity))
                {
                    if (state.WorldUnmanaged.IsServer())
                    {
                        UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} already has gathering component, skipping");
                    }
                    return;
                }

                // Check cooldown - don't trigger if still in cooldown period
                if (state.EntityManager.HasComponent<WorkerActionCooldownComponent>(unitEntity))
                {
                    WorkerActionCooldownComponent cooldown = state.EntityManager.GetComponentData<WorkerActionCooldownComponent>(unitEntity);
                    if (SystemAPI.Time.ElapsedTime < cooldown.CooldownEndTime)
                    {
                        if (state.WorldUnmanaged.IsServer())
                        {
                            double remainingTime = cooldown.CooldownEndTime - SystemAPI.Time.ElapsedTime;
                            UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} in cooldown, {remainingTime:F2}s remaining");
                        }
                        return;
                    }
                    else
                    {
                        // Cooldown expired, remove it
                        _entityCommandBuffer.RemoveComponent<WorkerActionCooldownComponent>(unitEntity);
                    }
                }

                // Don't trigger gathering if worker has resources to store (needs to go to Town Center first)
                if (state.EntityManager.HasComponent<CurrentWorkerResourceQuantityComponent>(unitEntity))
                {
                    CurrentWorkerResourceQuantityComponent resourceQuantity = state.EntityManager.GetComponentData<CurrentWorkerResourceQuantityComponent>(unitEntity);
                    if (resourceQuantity.Value > 0)
                    {
                        if (state.WorldUnmanaged.IsServer())
                        {
                            UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} has {resourceQuantity.Value} resources to store, skipping gathering trigger");
                        }
                        return;
                    }
                }

                if (state.WorldUnmanaged.IsServer())
                {
                    bool hasGhostOwner = state.EntityManager.HasComponent<Unity.NetCode.GhostOwner>(unitEntity);
                    int ghostOwnerId = hasGhostOwner ? state.EntityManager.GetComponentData<Unity.NetCode.GhostOwner>(unitEntity).NetworkId : -1;
                    UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index}:{unitEntity.Version} (GhostOwner: {ghostOwnerId}) reached resource {targetEntity.Index}:{targetEntity.Version}, setting IsFollowingTarget=false, adding gathering component (Frame: {UnityEngine.Time.frameCount})");
                }

                _entityCommandBuffer.AddComponent(unitEntity, GetGatheringComponent(targetEntity));

                // Reset resource quantity - add if doesn't exist, otherwise set to 0
                CurrentWorkerResourceQuantityComponent quantityComponent = GetGatheringQuantityComponent(targetEntity);
                if (!state.EntityManager.HasComponent<CurrentWorkerResourceQuantityComponent>(unitEntity))
                {
                    _entityCommandBuffer.AddComponent(unitEntity, quantityComponent);
                }
                else
                {
                    _entityCommandBuffer.SetComponent(unitEntity, quantityComponent);
                }

                selectedTarget.IsFollowingTarget = false;
                _entityCommandBuffer.SetComponent(unitEntity, selectedTarget);

                // Mark this entity as having a pending component
                _entitiesWithPendingComponents.Add(unitEntity);
                return;
            }

            if (_buildingLookup.HasComponent(targetEntity))
            {
                SetWorkerBuildingProcess(unitEntity, targetEntity, ref selectedTarget, unitTeam);
                return;
            }

            UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} reached unknown target {targetEntity.Index}, setting IsFollowingTarget=false");
            selectedTarget.IsFollowingTarget = false;
            selectedTarget.TargetEntity = Entity.Null;
            _entityCommandBuffer.SetComponent(unitEntity, selectedTarget);
        }

        private CurrentWorkerResourceQuantityComponent GetGatheringQuantityComponent(Entity targetEntity)
        {
            ResourceTypeComponent resourceTypeComponent = _resourceLookup[targetEntity];
            return new CurrentWorkerResourceQuantityComponent
            {
                ResourceType = resourceTypeComponent.Type,
                Value = 0,
                PreviousResourceEntity = Entity.Null
            };
        }

        private void SetWorkerBuildingProcess(Entity unitEntity, Entity targetEntity, ref UnitSelectedTargetComponent selectedTarget,
            TeamType unitTeam)
        {
            TeamType buildingTeam = _teamLookup[targetEntity].Team;
            if (buildingTeam != unitTeam)
            {
                return;
            }

            if (_constructionProgressLookup.HasComponent(targetEntity))
            {
                _entityCommandBuffer.AddComponent(unitEntity, GetBuildingComponent(targetEntity));
                selectedTarget.IsFollowingTarget = false;
                _entityCommandBuffer.SetComponent(unitEntity, selectedTarget);
                _entitiesWithPendingComponents.Add(unitEntity);
                return;
            }

            if (IsWorkerStoreResourcesAvailable(targetEntity, unitEntity))
            {
                UnityEngine.Debug.Log($"[TRIGGER] Worker {unitEntity.Index} reached Town Center {targetEntity.Index}, adding storing component");
                _entityCommandBuffer.AddComponent(unitEntity, GetStoringComponent(targetEntity));
                selectedTarget.IsFollowingTarget = false;
                _entityCommandBuffer.SetComponent(unitEntity, selectedTarget);
                _entitiesWithPendingComponents.Add(unitEntity);
            }
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
            if (!_currentResourceQuantityLookup.TryGetComponent(unitEntity, out CurrentWorkerResourceQuantityComponent resourceQuantity))
            {
                return false;
            }

            if (!_buildingTypeLookup.TryGetComponent(targetEntity, out BuildingTypeComponent buildingType))
            {
                return false;
            }

            return resourceQuantity.Value > 0 && buildingType.Type == BuildingType.Center;
        }

        private WorkerGatheringTagComponent GetGatheringComponent(Entity selectedTargetTargetEntity)
        {
            return new WorkerGatheringTagComponent
            {
                ResourceEntity = selectedTargetTargetEntity
            };
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

