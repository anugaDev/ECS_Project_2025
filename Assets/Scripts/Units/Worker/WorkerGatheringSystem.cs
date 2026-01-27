using Buildings;
using ElementCommons;
using GatherableResources;
using PlayerInputs;
using Types;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.Worker
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct WorkerGatheringSystem : ISystem
    {
        private const float MAX_GATHERING_AMOUNT = 50.0f;

        private const float GATHERING_DISTANCE_THRESHOLD = 2.0f;

        private const float GATHERING_RATE_PER_SECOND = 5.0f;

        private ComponentLookup<CurrentResourceQuantityComponent> _resourceQuantityLookup;

        private ComponentLookup<ResourceTypeComponent> _resourceTypeLookup;

        private ComponentLookup<BuildingTypeComponent> _buildingTypeLookup;

        private ComponentLookup<ElementTeamComponent> _teamLookup;

        private ComponentLookup<LocalTransform> _transformLookup;

        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _resourceTypeLookup = state.GetComponentLookup<ResourceTypeComponent>(true);
            _resourceQuantityLookup = state.GetComponentLookup<CurrentResourceQuantityComponent>();
            _buildingTypeLookup = state.GetComponentLookup<BuildingTypeComponent>(true);
            _teamLookup = state.GetComponentLookup<ElementTeamComponent>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _resourceTypeLookup.Update(ref state);
            _resourceQuantityLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _teamLookup.Update(ref state);
            _transformLookup.Update(ref state);

            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRO<LocalTransform> workerTransform, RefRO<WorkerGatheringTagComponent> gatheringTag,
                     RefRW<CurrentResourceQuantityComponent> workerResource, RefRO<ElementTeamComponent> workerTeam,
                     Entity workerEntity) in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRO<WorkerGatheringTagComponent>, RefRW<CurrentResourceQuantityComponent>,
                                       RefRO<ElementTeamComponent>>().WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                ProcessGathering(workerTransform.ValueRO, gatheringTag.ValueRO,
                               ref workerResource.ValueRW, workerTeam.ValueRO.Team,
                               workerEntity, deltaTime, ref state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessGathering(LocalTransform workerTransform, WorkerGatheringTagComponent gatheringTag,
                                     ref CurrentResourceQuantityComponent workerResource, TeamType workerTeam,
                                     Entity workerEntity, float deltaTime, ref SystemState state)
        {
            Entity resourceEntity = gatheringTag.ResourceEntity;

            if (!state.EntityManager.Exists(resourceEntity))
            {
                _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
                return;
            }

            if (!_resourceQuantityLookup.TryGetComponent(resourceEntity, out CurrentResourceQuantityComponent resourceQuantity))
            {
                _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
                return;
            }

            if (resourceQuantity.Value <= 0)
            {
                _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
                return;
            }

            LocalTransform resourceTransform = state.EntityManager.GetComponentData<LocalTransform>(resourceEntity);
            float distanceSq = math.distancesq(workerTransform.Position, resourceTransform.Position);

            if (distanceSq > GATHERING_DISTANCE_THRESHOLD * GATHERING_DISTANCE_THRESHOLD)
            {
                return;
            }

            if (workerResource.ResourceType == ResourceType.None &&
                _resourceTypeLookup.TryGetComponent(resourceEntity, out ResourceTypeComponent resourceType))
            {
                workerResource.ResourceType = resourceType.Type;
            }

            float gatherAmount = GATHERING_RATE_PER_SECOND * deltaTime;
            int amountToGather = (int)math.min(gatherAmount, resourceQuantity.Value);
            amountToGather = (int)math.min(amountToGather, MAX_GATHERING_AMOUNT - workerResource.Value);

            if (amountToGather > 0)
            {
                workerResource.Value += amountToGather;
                resourceQuantity.Value -= amountToGather;
                _entityCommandBuffer.SetComponent(resourceEntity, resourceQuantity);

                UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} gathered {amountToGather} {workerResource.ResourceType} from resource {resourceEntity.Index}. Current: {workerResource.Value}/{MAX_GATHERING_AMOUNT}");
            }

            if (!(workerResource.Value >= MAX_GATHERING_AMOUNT)) return;

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} reached max capacity ({MAX_GATHERING_AMOUNT}). Moving to Town Center...");

            _entityCommandBuffer.AddComponent(workerEntity, new PreviousResourceEntityComponent
            {
                ResourceEntity = resourceEntity
            });

            FindAndMoveToClosestTownCenter(workerTransform.Position, workerTeam, workerEntity, ref state);
            _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
        }

        private void FindAndMoveToClosestTownCenter(float3 workerPosition, TeamType workerTeam,
                                                    Entity workerEntity, ref SystemState state)
        {
            Entity closestTownCenter = Entity.Null;
            float closestDistanceSq = float.MaxValue;

            foreach ((RefRO<BuildingTypeComponent> buildingType, RefRO<ElementTeamComponent> buildingTeam,
                     RefRO<LocalTransform> buildingTransform, Entity buildingEntity)
                     in SystemAPI.Query<RefRO<BuildingTypeComponent>,
                                       RefRO<ElementTeamComponent>, RefRO<LocalTransform>>()
                         .WithAll<BuildingComponents>().WithEntityAccess())
            {
                if (buildingType.ValueRO.Type != BuildingType.Center ||
                    buildingTeam.ValueRO.Team != workerTeam)
                {
                    continue;
                }

                float distanceSq = math.distancesq(workerPosition, buildingTransform.ValueRO.Position);

                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestTownCenter = buildingEntity;
                }
            }

            if (closestTownCenter != Entity.Null)
            {
                SetMovementTarget(workerEntity, closestTownCenter, ref state);
            }
        }

        private void SetMovementTarget(Entity workerEntity, Entity targetEntity, ref SystemState state)
        {
            if (!_transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                return;
            }

            SetTargetPosition(workerEntity, targetTransform);
            SetStoringTarget(workerEntity, targetEntity);
            ClearPathBuffer(workerEntity, state);
        }

        private void SetTargetPosition(Entity workerEntity, LocalTransform targetTransform)
        {
            UnitTargetPositionComponent targetPosition = new UnitTargetPositionComponent
            {
                Value = targetTransform.Position,
                MustMove = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, targetPosition);
        }

        private void SetStoringTarget(Entity workerEntity, Entity targetEntity)
        {
            UnitSelectedTargetComponent selectedTarget = new UnitSelectedTargetComponent
            {
                TargetEntity = targetEntity,
                IsFollowingTarget = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, selectedTarget);
        }

        private void ClearPathBuffer(Entity workerEntity, SystemState state)
        {
            PathComponent pathComp = state.EntityManager.GetComponentData<PathComponent>(workerEntity);
            pathComp.HasPath = false;
            pathComp.CurrentWaypointIndex = 0;
            pathComp.LastTargetPosition = float3.zero;
            _entityCommandBuffer.SetComponent(workerEntity, pathComp);
            DynamicBuffer<PathWaypointBuffer> pathBuffer = state.EntityManager.GetBuffer<PathWaypointBuffer>(workerEntity);
            pathBuffer.Clear();
        }
    }
}

