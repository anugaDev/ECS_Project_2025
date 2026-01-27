using PlayerInputs;
using Types;
using UI;
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
    public partial struct WorkerStoringSystem : ISystem
    {
        private const float STORING_DISTANCE_THRESHOLD = 3.0f;

        private ComponentLookup<PreviousResourceEntityComponent> _previousResourceLookup;
        
        private ComponentLookup<CurrentWoodComponent> _woodLookup;

        private ComponentLookup<CurrentFoodComponent> _foodLookup;
        
        private ComponentLookup<LocalTransform> _transformLookup;

        private ComponentLookup<GhostOwner> _ghostOwnerLookup;

        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _woodLookup = state.GetComponentLookup<CurrentWoodComponent>();
            _foodLookup = state.GetComponentLookup<CurrentFoodComponent>();
            _ghostOwnerLookup = state.GetComponentLookup<GhostOwner>(true);
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _previousResourceLookup = state.GetComponentLookup<PreviousResourceEntityComponent>(true);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _woodLookup.Update(ref state);
            _foodLookup.Update(ref state);
            _ghostOwnerLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _previousResourceLookup.Update(ref state);

            foreach ((RefRO<LocalTransform> workerTransform, RefRO<WorkerStoringTagComponent> storingTag,
                     RefRW<CurrentResourceQuantityComponent> workerResource, RefRO<GhostOwner> workerOwner,
                     Entity workerEntity) in SystemAPI.Query<RefRO<LocalTransform>, 
                             RefRO<WorkerStoringTagComponent>,RefRW<CurrentResourceQuantityComponent>,
                                       RefRO<GhostOwner>>().WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                ProcessStoring(workerTransform.ValueRO, storingTag.ValueRO, 
                             ref workerResource.ValueRW, workerOwner.ValueRO,
                             workerEntity, ref state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessStoring(LocalTransform workerTransform, WorkerStoringTagComponent storingTag,
                                   ref CurrentResourceQuantityComponent workerResource,GhostOwner workerOwner,
                                   Entity workerEntity,ref SystemState state)
        {
            Entity buildingEntity = storingTag.BuildingEntity;

            if (!state.EntityManager.Exists(buildingEntity) || workerResource.Value <= 0)
            {
                _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);
                return;
            }

            LocalTransform buildingTransform = state.EntityManager.GetComponentData<LocalTransform>(buildingEntity);
            float distanceSq = math.distancesq(workerTransform.Position, buildingTransform.Position);

            if (distanceSq > STORING_DISTANCE_THRESHOLD * STORING_DISTANCE_THRESHOLD)
            {
                return;
            }

            Entity playerEntity = FindPlayerEntity(workerOwner.NetworkId, ref state);
            
            if (playerEntity == Entity.Null)
            {
                return;
            }

            StoreResourceToPlayer(playerEntity, workerResource.ResourceType, workerResource.Value);

            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} stored {workerResource.Value} {workerResource.ResourceType} at Town Center {buildingEntity.Index}");

            workerResource.Value = 0;
            workerResource.ResourceType = ResourceType.None;
            _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);

            if (_previousResourceLookup.TryGetComponent(workerEntity, out PreviousResourceEntityComponent previousResource))
            {
                Entity resourceEntity = previousResource.ResourceEntity;

                if (resourceEntity != Entity.Null && state.EntityManager.Exists(resourceEntity))
                {
                    UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} returning to previous resource {resourceEntity.Index}");
                    ReturnToGathering(workerEntity, resourceEntity, ref state);
                }
                else
                {
                    UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} previous resource no longer exists, going idle");
                    MakeWorkerIdle(workerEntity);
                }

                _entityCommandBuffer.RemoveComponent<PreviousResourceEntityComponent>(workerEntity);
            }
            else
            {
                UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} has no previous resource, going idle");
                MakeWorkerIdle(workerEntity);
            }
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

        private void ReturnToGathering(Entity workerEntity, Entity resourceEntity, ref SystemState state)
        {
            if (!_transformLookup.TryGetComponent(resourceEntity, out LocalTransform resourceTransform))
            {
                MakeWorkerIdle(workerEntity);
                return;
            }

            UnitTargetPositionComponent targetPosition = new UnitTargetPositionComponent
            {
                Value = resourceTransform.Position,
                MustMove = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, targetPosition);
            UnitSelectedTargetComponent selectedTarget = new UnitSelectedTargetComponent
            {
                TargetEntity = resourceEntity,
                IsFollowingTarget = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, selectedTarget);

            PathComponent pathComp = state.EntityManager.GetComponentData<PathComponent>(workerEntity);
            pathComp.HasPath = false;
            pathComp.CurrentWaypointIndex = 0;
            pathComp.LastTargetPosition = float3.zero;
            _entityCommandBuffer.SetComponent(workerEntity, pathComp);
            DynamicBuffer<PathWaypointBuffer> pathBuffer = state.EntityManager.GetBuffer<PathWaypointBuffer>(workerEntity);
            pathBuffer.Clear();
        }

        private void MakeWorkerIdle(Entity workerEntity)
        {
            UnitSelectedTargetComponent selectedTarget = new UnitSelectedTargetComponent
            {
                TargetEntity = Entity.Null,
                IsFollowingTarget = false
            };

            _entityCommandBuffer.SetComponent(workerEntity, selectedTarget);
        }
    }
}

