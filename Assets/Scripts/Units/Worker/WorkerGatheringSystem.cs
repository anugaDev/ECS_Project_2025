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
    [UpdateBefore(typeof(MovementSystems.UnitTargetActionTriggerSystem))]
    [BurstCompile]
    public partial struct WorkerGatheringSystem : ISystem
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
            // Only server should modify worker resource gathering to avoid desync
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
            _resourceTypeLookup.Update(ref state);
            _resourceQuantityLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _teamLookup.Update(ref state);
            _transformLookup.Update(ref state);

            int workerCount = 0;
            foreach ((RefRO<LocalTransform> workerTransform, RefRO<WorkerGatheringTagComponent> gatheringTag,
                     RefRW<CurrentWorkerResourceQuantityComponent> workerResource, RefRO<ElementTeamComponent> workerTeam,
                     Entity workerEntity) in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRO<WorkerGatheringTagComponent>, RefRW<CurrentWorkerResourceQuantityComponent>,
                                       RefRO<ElementTeamComponent>>().WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                workerCount++;
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

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index}:{workerEntity.Version} reached max capacity ({MAX_GATHERING_AMOUNT}). Transitioning to storing...");
            workerResource.PreviousResourceEntity = resourceEntity;
            _entityCommandBuffer.RemoveComponent<WorkerGatheringTagComponent>(workerEntity);

            // Add cooldown to prevent immediate re-triggering (1 second cooldown)
            _entityCommandBuffer.AddComponent(workerEntity, new WorkerActionCooldownComponent
            {
                CooldownEndTime = SystemAPI.Time.ElapsedTime + 1.0
            });
        }

        private Entity FindAndMoveToClosestTownCenter(float3 workerPosition, TeamType workerTeam,
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
                UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} found Town Center {closestTownCenter.Index}, setting movement target (Frame: {UnityEngine.Time.frameCount})");
                SetMovementTarget(workerEntity, closestTownCenter, ref state);
            }
            else
            {
                UnityEngine.Debug.LogError($"[GATHERING] Worker {workerEntity.Index} could not find any Town Center!");
            }

            return closestTownCenter;
        }

        private void SetMovementTarget(Entity workerEntity, Entity targetEntity, ref SystemState state)
        {
            if (!_transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                UnityEngine.Debug.LogWarning($"[GATHERING] Worker {workerEntity.Index} cannot find transform for Town Center {targetEntity.Index}");
                return;
            }

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} setting movement target to Town Center {targetEntity.Index} at position {targetTransform.Position}");

            // IMPORTANT: We CANNOT modify IInputComponentData (UnitTargetPositionComponent) from server-side systems!
            // IInputComponentData flows FROM client TO server, not the other way.
            // Instead, we use ServerMoveOverrideComponent which will be applied by ServerMoveOverrideSystem
            // running in GhostInputSystemGroup where it CAN modify IInputComponentData properly.

            ServerMoveOverrideComponent serverOverride = new ServerMoveOverrideComponent
            {
                TargetPosition = targetTransform.Position,
                TargetEntity = targetEntity,
                IsFollowingTarget = true,
                StoppingDistance = 2.0f, // Town Center stopping distance
                IsActive = true
            };

            // Add or set the server override component
            if (state.EntityManager.HasComponent<ServerMoveOverrideComponent>(workerEntity))
            {
                _entityCommandBuffer.SetComponent(workerEntity, serverOverride);
            }
            else
            {
                _entityCommandBuffer.AddComponent(workerEntity, serverOverride);
            }

            UnityEngine.Debug.Log($"[GATHERING] Worker {workerEntity.Index} queued server movement override to Town Center {targetEntity.Index} at {targetTransform.Position}");
        }
    }
}

