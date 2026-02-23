using Buildings;
using ElementCommons;
using Types;
using UI;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

namespace Units.Worker
{
    // DISABLED FOR TESTING - Testing basic movement only
    //[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    //[UpdateAfter(typeof(MovementSystems.UnitStateSystem))]
    //[BurstCompile]
    public partial struct WorkerStoringSystem_DISABLED : ISystem
    {
        private const float STORING_DISTANCE_THRESHOLD = 3.0f;

        private ComponentLookup<CurrentWoodComponent> _woodLookup;

        private ComponentLookup<CurrentFoodComponent> _foodLookup;
        
        private ComponentLookup<LocalTransform> _transformLookup;

        private ComponentLookup<GhostOwner> _ghostOwnerLookup;

        private ComponentLookup<BuildingTypeComponent> _buildingTypeLookup;

        private ComponentLookup<ElementTeamComponent> _teamLookup;

        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _woodLookup = state.GetComponentLookup<CurrentWoodComponent>();
            _foodLookup = state.GetComponentLookup<CurrentFoodComponent>();
            _ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _buildingTypeLookup = state.GetComponentLookup<BuildingTypeComponent>(true);
            _teamLookup = state.GetComponentLookup<ElementTeamComponent>(true);
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
            _woodLookup.Update(ref state);
            _foodLookup.Update(ref state);
            _ghostOwnerLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _teamLookup.Update(ref state);

            foreach ((RefRO<LocalTransform> workerTransform,
                     RefRO<WorkerStoringTagComponent> storingTag,
                     RefRO<CurrentTargetComponent> currentTarget,
                     RefRW<CurrentWorkerResourceQuantityComponent> workerResource,
                     RefRO<GhostOwner> workerOwner,
                     Entity workerEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRO<WorkerStoringTagComponent>,
                                       RefRO<CurrentTargetComponent>,
                                       RefRW<CurrentWorkerResourceQuantityComponent>,
                                       RefRO<GhostOwner>>()
                         .WithAll<Simulate, UnitTagComponent>()
                         .WithEntityAccess())
            {
                if (!currentTarget.ValueRO.IsTargetReached)
                    continue;

                UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} reached Town Center, storing resources");
                ProcessStoring(workerTransform.ValueRO, storingTag.ValueRO,
                             ref workerResource.ValueRW, workerOwner.ValueRO,
                             workerEntity, ref state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessStoring(LocalTransform workerTransform, WorkerStoringTagComponent storingTag,
                                   ref CurrentWorkerResourceQuantityComponent workerResource, GhostOwner workerOwner,
                                   Entity workerEntity, ref SystemState state)
        {
            Entity buildingEntity = storingTag.BuildingEntity;
            if (buildingEntity == Entity.Null || !state.EntityManager.Exists(buildingEntity))
            {
                UnityEngine.Debug.LogWarning($"[STORING] Worker {workerEntity.Index} target building no longer exists");
                _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);
                return;
            }

            if (workerResource.Value <= 0)
            {
                UnityEngine.Debug.LogWarning($"[STORING] Worker {workerEntity.Index} has no resources to store");
                _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);
                return;
            }

            Entity playerEntity = FindPlayerEntity(workerOwner.NetworkId, ref state);
            if (playerEntity == Entity.Null)
            {
                UnityEngine.Debug.LogError($"[STORING] Worker {workerEntity.Index} cannot find player entity");
                return;
            }

            StoreResourceToPlayer(playerEntity, workerResource.ResourceType, workerResource.Value);
            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} stored {workerResource.Value} {workerResource.ResourceType} at Town Center {buildingEntity.Index}");

            workerResource.Value = 0;
            workerResource.ResourceType = ResourceType.None;
            _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);
            Entity previousResourceEntity = workerResource.PreviousResourceEntity;

            if (previousResourceEntity != Entity.Null && state.EntityManager.Exists(previousResourceEntity))
            {
                UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} returning to previous resource {previousResourceEntity.Index}");
                SetNextTarget(workerEntity, previousResourceEntity, ref state);
                _entityCommandBuffer.AddComponent(workerEntity, new WorkerGatheringTagComponent
                {
                    ResourceEntity = previousResourceEntity
                });

                workerResource.PreviousResourceEntity = Entity.Null;
            }
            else
            {
                UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} has no previous resource, going idle");
                workerResource.PreviousResourceEntity = Entity.Null;
            }
        }

        private void SetNextTarget(Entity workerEntity, Entity targetEntity, ref SystemState state)
        {
            if (!_transformLookup.TryGetComponent(targetEntity, out LocalTransform targetTransform))
            {
                UnityEngine.Debug.LogWarning($"[STORING] Worker {workerEntity.Index} cannot find transform for target {targetEntity.Index}");
                return;
            }

            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} setting next target to {targetEntity.Index} at {targetTransform.Position}");
            SetServerStateTargetComponent serverTarget = new SetServerStateTargetComponent
            {
                TargetEntity = targetEntity,
                TargetPosition = targetTransform.Position,
                IsFollowingTarget = true,
                StoppingDistance = 2.0f,
                HasNewTarget = true
            };

            _entityCommandBuffer.SetComponent(workerEntity, serverTarget);
        }

        private Entity FindPlayerEntity(int networkId, ref SystemState state)
        {
            foreach ((RefRO<GhostOwner> ghostOwner, Entity playerEntity) 
                     in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<PlayerTagComponent>().WithEntityAccess())
            {
                if (ghostOwner.ValueRO.NetworkId != networkId)
                {
                    continue;
                }

                return playerEntity;
            }

            return Entity.Null;
        }

        private void StoreResourceToPlayer(Entity playerEntity, ResourceType resourceType, int amount)
        {
            switch (resourceType)
            {
                case ResourceType.Wood:
                    if (_woodLookup.TryGetComponent(playerEntity, out CurrentWoodComponent wood))
                    {
                        wood.Value += amount;
                        _entityCommandBuffer.SetComponent(playerEntity, wood);
                    }
                    break;
                    
                case ResourceType.Food:
                    if (_foodLookup.TryGetComponent(playerEntity, out CurrentFoodComponent food))
                    {
                        food.Value += amount;
                        _entityCommandBuffer.SetComponent(playerEntity, food);
                    }
                    break;
            }

            _entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
        }
    }
}

