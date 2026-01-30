using Buildings;
using ElementCommons;
using PlayerInputs;
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
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(MovementSystems.UnitTargetActionTriggerSystem))]
    [BurstCompile]
    public partial struct WorkerStoringSystem : ISystem
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
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            if (!networkTime.IsFirstTimeFullyPredictingTick)
            {
                return;
            }

            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _woodLookup.Update(ref state);
            _foodLookup.Update(ref state);
            _ghostOwnerLookup.Update(ref state);
            _transformLookup.Update(ref state);
            _buildingTypeLookup.Update(ref state);
            _teamLookup.Update(ref state);

            int storingWorkerCount = 0;
            foreach ((RefRO<LocalTransform> workerTransform, RefRO<WorkerStoringTagComponent> storingTag,
                     RefRW<CurrentWorkerResourceQuantityComponent> workerResource, RefRO<GhostOwner> workerOwner,
                     Entity workerEntity) in SystemAPI.Query<RefRO<LocalTransform>,
                             RefRO<WorkerStoringTagComponent>,RefRW<CurrentWorkerResourceQuantityComponent>,
                                       RefRO<GhostOwner>>().WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                storingWorkerCount++;
                UnityEngine.Debug.Log($"[STORING-SYSTEM] Processing worker {workerEntity.Index} with storing tag (BuildingEntity: {storingTag.ValueRO.BuildingEntity.Index}, Resources: {workerResource.ValueRO.Value}) (Frame: {UnityEngine.Time.frameCount})");
                ProcessStoring(workerTransform.ValueRO, storingTag.ValueRO,
                             ref workerResource.ValueRW, workerOwner.ValueRO,
                             workerEntity, ref state);
            }

            if (storingWorkerCount > 0)
            {
                UnityEngine.Debug.Log($"[STORING-SYSTEM] Processed {storingWorkerCount} workers with storing tag (Frame: {UnityEngine.Time.frameCount})");
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessStoring(LocalTransform workerTransform, WorkerStoringTagComponent storingTag,
                                   ref CurrentWorkerResourceQuantityComponent workerResource,GhostOwner workerOwner,
                                   Entity workerEntity,ref SystemState state)
        {
            Entity buildingEntity = storingTag.BuildingEntity;

            // If no building entity is set, find the closest Town Center
            if (buildingEntity == Entity.Null || !state.EntityManager.Exists(buildingEntity))
            {
                SetClosestCenterAsStoring(workerTransform, workerOwner, workerEntity, ref state);
                return;
            }

            Debug.Log(workerEntity.Index + "Storing entity in target "+ buildingEntity.Index +" "+"(Frame: {UnityEngine.Time.frameCount})");
            
            if (workerResource.Value <= 0)
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

            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index}:{workerEntity.Version} stored {workerResource.Value} {workerResource.ResourceType} at Town Center {buildingEntity.Index} (Frame: {UnityEngine.Time.frameCount})");

            workerResource.Value = 0;
            workerResource.ResourceType = ResourceType.None;
            _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);

            // Check if worker has a previous resource to return to
            Entity previousResourceEntity = workerResource.PreviousResourceEntity;
            bool hasPreviousResource = previousResourceEntity != Entity.Null;
            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index}:{workerEntity.Version} has previous resource: {hasPreviousResource} (Entity: {previousResourceEntity.Index}, Frame: {UnityEngine.Time.frameCount})");

            if (hasPreviousResource && state.EntityManager.Exists(previousResourceEntity))
            {
                UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index}:{workerEntity.Version} returning to previous resource {previousResourceEntity.Index}");
                ReturnToGathering(workerEntity, previousResourceEntity, ref state);

                // Clear the previous resource entity
                workerResource.PreviousResourceEntity = Entity.Null;
            }
            else
            {
                if (hasPreviousResource)
                {
                    UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} previous resource no longer exists, going idle");
                }
                else
                {
                    UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index} has no previous resource, going idle");
                }
                MakeWorkerIdle(workerEntity);
                workerResource.PreviousResourceEntity = Entity.Null;
            }
        }

        private void SetClosestCenterAsStoring(LocalTransform workerTransform, GhostOwner workerOwner, Entity workerEntity,
            ref SystemState state)
        {
            Entity buildingEntity;
            buildingEntity = FindClosestTownCenter(workerTransform.Position, workerOwner.NetworkId, ref state);

            if (buildingEntity == Entity.Null)
            {
                UnityEngine.Debug.LogWarning($"[STORING] Worker {workerEntity.Index} cannot find Town Center, removing storing tag");
                _entityCommandBuffer.RemoveComponent<WorkerStoringTagComponent>(workerEntity);
                return;
            }

            _entityCommandBuffer.SetComponent(workerEntity, new WorkerStoringTagComponent
            {
                BuildingEntity = buildingEntity
            });

            LocalTransform buildingTransform = state.EntityManager.GetComponentData<LocalTransform>(buildingEntity);
            UnitTargetPositionComponent targetPosition = new UnitTargetPositionComponent
            {
                Value = buildingTransform.Position,
                MustMove = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, targetPosition);

            UnitSelectedTargetComponent selectedTarget = new UnitSelectedTargetComponent
            {
                TargetEntity = buildingEntity,
                IsFollowingTarget = true
            };
            _entityCommandBuffer.SetComponent(workerEntity, selectedTarget);

            PathComponent pathComp = state.EntityManager.GetComponentData<PathComponent>(workerEntity);
            pathComp.HasPath = false;
            pathComp.CurrentWaypointIndex = 0;
            _entityCommandBuffer.SetComponent(workerEntity, pathComp);
            return;
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
            UnityEngine.Debug.Log($"[STORING] Worker {workerEntity.Index}:{workerEntity.Version} setting IsFollowingTarget=true, TargetEntity={resourceEntity.Index}, MustMove=true (Frame: {UnityEngine.Time.frameCount})");
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

        private Entity FindClosestTownCenter(float3 workerPosition, int workerNetworkId, ref SystemState state)
        {
            Entity closestTownCenter = Entity.Null;
            float closestDistanceSq = float.MaxValue;

            // Find worker's team by looking up the worker's GhostOwner
            TeamType workerTeam = TeamType.None;

            // Query all players to find the one matching the worker's network ID
            foreach ((RefRO<GhostOwner> ghostOwner, RefRO<ElementTeamComponent> team) in
                     SystemAPI.Query<RefRO<GhostOwner>, RefRO<ElementTeamComponent>>()
                         .WithAll<PlayerTagComponent>())
            {
                if (ghostOwner.ValueRO.NetworkId == workerNetworkId)
                {
                    workerTeam = team.ValueRO.Team;
                    break;
                }
            }

            if (workerTeam == TeamType.None)
            {
                UnityEngine.Debug.LogWarning($"[STORING] Could not find team for worker with NetworkId {workerNetworkId}");
                return Entity.Null;
            }

            // Find closest Town Center of the same team
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

            return closestTownCenter;
        }
    }
}

