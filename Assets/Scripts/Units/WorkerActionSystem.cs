using GatherableResources;
using Types;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Units.MovementSystems;
using Units.Worker;
using Unity.Mathematics;

namespace Units
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UnitStateSystem))]
    [UpdateBefore(typeof(Worker.WorkerGatheringSystem))]
    public partial class WorkerActionSystem : SystemBase
    {
        private ComponentLookup<ResourceTypeComponent> _resourceTypeLookup;

        protected override void OnCreate()
        {
            _resourceTypeLookup = GetComponentLookup<ResourceTypeComponent>(true);
            RequireForUpdate<UnitTagComponent>();
        }

        protected override void OnUpdate()
        {

            _resourceTypeLookup.Update(this);

            var ecb = new EntityCommandBuffer(Allocator.Temp);

            // --- Phase 1: Cancel existing worker actions when the player manually overrides ---
            // Player clicks bump SetInputStateTargetComponent.TargetVersion higher than
            // SetServerStateTargetComponent.TargetVersion. Detect that mismatch here.
            foreach ((RefRO<UnitTypeComponent>            unitType,
                      RefRO<SetInputStateTargetComponent> inputTarget,
                      RefRW<SetServerStateTargetComponent> serverTarget,
                      Entity                              entity)
                     in SystemAPI.Query<RefRO<UnitTypeComponent>,
                                       RefRO<SetInputStateTargetComponent>,
                                       RefRW<SetServerStateTargetComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                if (unitType.ValueRO.Type != UnitType.Worker)
                    continue;

                // Input version > server version → player issued a manual command
                if (inputTarget.ValueRO.TargetVersion <= serverTarget.ValueRO.TargetVersion)
                    continue;

                // Player manually overrode — cancel any active worker action
                if (SystemAPI.HasComponent<WorkerGatheringTagComponent>(entity))
                    ecb.RemoveComponent<WorkerGatheringTagComponent>(entity);
                if (SystemAPI.HasComponent<WorkerStoringTagComponent>(entity))
                    ecb.RemoveComponent<WorkerStoringTagComponent>(entity);
                if (SystemAPI.HasComponent<WorkerConstructionTagComponent>(entity))
                    ecb.RemoveComponent<WorkerConstructionTagComponent>(entity);

                // Equalize versions to prevent re-triggering every tick
                serverTarget.ValueRW.TargetVersion = inputTarget.ValueRO.TargetVersion;
            }

            // --- Phase 2: Assign gathering when an idle worker is near a resource ---

            foreach ((RefRO<UnitTypeComponent>            unitType,
                      RefRO<UnitStateComponent>           unitState,
                      RefRW<SetInputStateTargetComponent> inputTarget,
                      Entity                              entity)
                     in SystemAPI.Query<RefRO<UnitTypeComponent>,
                                       RefRO<UnitStateComponent>,
                                       RefRW<SetInputStateTargetComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithNone<WorkerGatheringTagComponent,
                                   WorkerStoringTagComponent,
                                   WorkerConstructionTagComponent>()
                         .WithEntityAccess())
            {
                if (unitType.ValueRO.Type != UnitType.Worker)
                    continue;

                if (unitState.ValueRO.State != UnitState.Idle)
                    continue;

                UnityEngine.Debug.Log($"[WAS-P2] Worker {entity.Index} IDLE. IsFollowing={inputTarget.ValueRO.IsFollowingTarget}, TargetEntity={inputTarget.ValueRO.TargetEntity}, TargetPos={inputTarget.ValueRO.TargetPosition}, TV_in={inputTarget.ValueRO.TargetVersion}");

                Entity targetEntity = inputTarget.ValueRO.TargetEntity;

                // If the client clicked a target, but the Entity ID failed to sync over NetCode
                // (e.g., baked subscene resources lacking Ghost IDs), TargetEntity arrives as Null.
                // We fallback to a spatial search near the clicked TargetPosition.
                if (inputTarget.ValueRO.IsFollowingTarget && (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity)))
                {
                    float3 targetPos = inputTarget.ValueRO.TargetPosition;
                    float closestDistSq = float.MaxValue;
                    Entity closestResource = Entity.Null;

                    foreach ((RefRO<Unity.Transforms.LocalTransform> resTransform, RefRO<ResourceTypeComponent> resType, Entity resEntity) 
                        in SystemAPI.Query<RefRO<Unity.Transforms.LocalTransform>, RefRO<ResourceTypeComponent>>().WithEntityAccess())
                    {
                        float distSq = Unity.Mathematics.math.distancesq(targetPos, resTransform.ValueRO.Position);
                        if (distSq < closestDistSq)
                        {
                            closestDistSq = distSq;
                            closestResource = resEntity;
                        }
                    }

                    UnityEngine.Debug.Log($"[WAS-P2] Spatial search at {targetPos}. Closest distSq={closestDistSq}, resource={closestResource.Index}");

                    if (closestResource != Entity.Null && closestDistSq <= 36.0f)
                    {
                        targetEntity = closestResource;
                    }
                }
                else if (!inputTarget.ValueRO.IsFollowingTarget)
                {
                    UnityEngine.Debug.Log($"[WAS-P2] IsFollowingTarget=FALSE, skipping spatial search");
                }

                if (targetEntity == Entity.Null || !EntityManager.Exists(targetEntity))
                {
                    UnityEngine.Debug.Log($"[WAS-P2] No valid target found, clearing IsFollowingTarget");
                    // Clear input so we don't infinitely search
                    inputTarget.ValueRW.IsFollowingTarget = false;
                    continue;
                }

                if (!_resourceTypeLookup.HasComponent(targetEntity))
                {
                    UnityEngine.Debug.Log($"[WAS-P2] Target {targetEntity.Index} has no ResourceTypeComponent");
                    inputTarget.ValueRW.TargetEntity = Entity.Null;
                    inputTarget.ValueRW.IsFollowingTarget = false;
                    continue;
                }

                UnityEngine.Debug.Log($"[WAS-P2] ✓ ASSIGNING GATHERING to worker {entity.Index} for resource {targetEntity.Index}");

                ecb.AddComponent(entity, new WorkerGatheringTagComponent
                {
                    ResourceEntity = targetEntity
                });

                // Clear input so this doesn't re-trigger next tick
                inputTarget.ValueRW.TargetEntity = Entity.Null;
                inputTarget.ValueRW.IsFollowingTarget = false;
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
