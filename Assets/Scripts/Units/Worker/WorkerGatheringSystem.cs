using Buildings;
using ElementCommons;
using GatherableResources;
using Types;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.Worker
{
    // DISABLED FOR TESTING - Testing basic movement only
    //[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(MovementSystems.UnitStateSystem))]
    //[BurstCompile]
    public partial struct WorkerGatheringSystem_DISABLED : ISystem
    {
        private const int MAX_GATHERING_AMOUNT = 50;

        private const float GATHERING_DISTANCE_THRESHOLD = 2.0f;

        private const int AMMOUNT_TO_GATHER = 1;

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
            if (!state.WorldUnmanaged.IsServer())
                return;

            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
            if (!networkTime.IsFirstTimeFullyPredictingTick)
                return;

            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _resourceTypeLookup.Update(ref state);
            _resourceQuantityLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _teamLookup.Update(ref state);
            _transformLookup.Update(ref state);

            foreach ((RefRO<LocalTransform> workerTransform,
                     RefRO<WorkerGatheringTagComponent> gatheringTag,
                     RefRO<CurrentTargetComponent> currentTarget,
                     RefRW<CurrentWorkerResourceQuantityComponent> workerResource,
                     RefRO<ElementTeamComponent> workerTeam,
                     Entity workerEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRO<WorkerGatheringTagComponent>,
                                       RefRO<CurrentTargetComponent>,
                                       RefRW<CurrentWorkerResourceQuantityComponent>,
                                       RefRO<ElementTeamComponent>>()
                         .WithAll<Simulate, UnitTagComponent>()
                         .WithEntityAccess())
            {
                if (!currentTarget.ValueRO.IsTargetReached)
                    continue;

                ProcessGathering(workerTransform.ValueRO, gatheringTag.ValueRO,
                               ref workerResource.ValueRW, workerTeam.ValueRO.Team,
                               workerEntity, ref state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessGathering(LocalTransform workerTransform, WorkerGatheringTagComponent gatheringTag,
                                     ref CurrentWorkerResourceQuantityComponent workerResource, TeamType workerTeam,
                                     Entity workerEntity, ref SystemState state)
        {
            Entity resourceEntity = gatheringTag.ResourceEntity;

            if (!state.EntityManager.Exists(resourceEntity) || !_resourceQuantityLookup.TryGetComponent(resourceEntity, out CurrentResourceQuantityComponent resourceQuantity) || resourceQuantity.Value <= 0)
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

            int amountToGather = math.min(AMMOUNT_TO_GATHER, resourceQuantity.Value);
            amountToGather = math.min(amountToGather, MAX_GATHERING_AMOUNT - workerResource.Value);

            if (amountToGather > 0)
            {
                workerResource.Value += amountToGather;
                resourceQuantity.Value -= amountToGather;
                _entityCommandBuffer.SetComponent(resourceEntity, resourceQuantity);
            }

            if (workerResource.Value < MAX_GATHERING_AMOUNT)
            {
                return;
            }

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} reached max capacity ({MAX_GATHERING_AMOUNT}). Finding Town Center...");
            workerResource.PreviousResourceEntity = resourceEntity;

            Entity townCenter = FindClosestTownCenter(workerTransform.Position, workerTeam, ref state);

            if (townCenter != Entity.Null)
            {
                SetNextTarget(workerEntity, townCenter, ref state);
                _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
                _entityCommandBuffer.AddComponent(workerEntity, new WorkerStoringTagComponent
                {
                    BuildingEntity = townCenter
                });
            }
            else
            {
                UnityEngine.Debug.LogError($"[GATHERING] Worker {workerEntity.Index} cannot find Town Center!");
                _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);
            }
        }

        private Entity FindClosestTownCenter(float3 workerPosition, TeamType workerTeam, ref SystemState state)
        {
            Entity closestTownCenter = Entity.Null;
            float closestDistanceSq = float.MaxValue;

            foreach ((RefRO<BuildingTypeComponent> buildingType,
                     RefRO<ElementTeamComponent> buildingTeam,
                     RefRO<LocalTransform> buildingTransform,
                     Entity buildingEntity)
                     in SystemAPI.Query<RefRO<BuildingTypeComponent>,
                                       RefRO<ElementTeamComponent>,
                                       RefRO<LocalTransform>>()
                         .WithAll<BuildingComponents>()
                         .WithEntityAccess())
            {
                if (buildingType.ValueRO.Type != BuildingType.Center ||
                    buildingTeam.ValueRO.Team != workerTeam)
                    continue;

                float distanceSq = math.distancesq(workerPosition, buildingTransform.ValueRO.Position);

                if (distanceSq < closestDistanceSq)
                {
                    closestDistanceSq = distanceSq;
                    closestTownCenter = buildingEntity;
                }
            }

            return closestTownCenter;
        }

        private void SetNextTarget(Entity workerEntity, Entity targetEntity, ref SystemState state)
        {
            if (!_transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                UnityEngine.Debug.LogWarning($"[GATHERING] Worker {workerEntity.Index} cannot find transform for target {targetEntity.Index}");
                return;
            }

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} setting next target to {targetEntity.Index} at {targetTransform.Position}");

            SetServerStateTargetComponent serverTarget = new SetServerStateTargetComponent
            {
                TargetEntity = targetEntity,
                TargetPosition = targetTransform.Position,
                IsFollowingTarget = true,
                StoppingDistance = 2.0f, // Town Center stopping distance
                HasNewTarget = true
            };

            _entityCommandBuffer.SetComponent(workerEntity, serverTarget);
        }
    }
}

