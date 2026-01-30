using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Units;

namespace PlayerInputs
{
    /// <summary>
    /// System that applies server-authoritative movement overrides to input components.
    /// Runs in GhostInputSystemGroup so it can properly modify IInputComponentData.
    /// </summary>
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [BurstCompile]
    public partial struct ServerMoveOverrideSystem : ISystem
    {
        private int _frameCounter;

        public void OnCreate(ref SystemState state)
        {
            UnityEngine.Debug.Log("[SERVER-OVERRIDE] System created!");
        }

        public void OnUpdate(ref SystemState state)
        {
            _frameCounter++;

            // Log every 60 frames to confirm system is running
            if (_frameCounter % 60 == 0)
            {
                UnityEngine.Debug.Log($"[SERVER-OVERRIDE] System is running (frame {_frameCounter})");
            }

            int entityCount = 0;
            int totalEntitiesWithComponent = 0;

            // First, count all entities with ServerMoveOverrideComponent
            foreach (var _ in SystemAPI.Query<RefRO<ServerMoveOverrideComponent>>())
            {
                totalEntitiesWithComponent++;
            }

            if (totalEntitiesWithComponent > 0)
            {
                UnityEngine.Debug.Log($"[SERVER-OVERRIDE] Found {totalEntitiesWithComponent} entities with ServerMoveOverrideComponent");
            }

            foreach ((RefRO<ServerMoveOverrideComponent> serverOverride,
                     RefRW<UnitTargetPositionComponent> targetPosition,
                     RefRW<UnitSelectedTargetComponent> selectedTarget,
                     RefRW<PathComponent> pathComponent,
                     Entity entity)
                     in SystemAPI.Query<RefRO<ServerMoveOverrideComponent>,
                                       RefRW<UnitTargetPositionComponent>,
                                       RefRW<UnitSelectedTargetComponent>,
                                       RefRW<PathComponent>>()
                         .WithAll<UnitTagComponent>()
                         .WithEntityAccess())
            {
                entityCount++;

                if (!serverOverride.ValueRO.IsActive)
                {
                    UnityEngine.Debug.LogWarning($"[SERVER-OVERRIDE] Found inactive override on entity {entity.Index}, skipping");
                    continue;
                }

                // Apply server override to input component
                targetPosition.ValueRW.Value = serverOverride.ValueRO.TargetPosition;
                targetPosition.ValueRW.MustMove = true;

                // Apply server-provided target entity to input component
                selectedTarget.ValueRW.TargetEntity = serverOverride.ValueRO.TargetEntity;
                selectedTarget.ValueRW.IsFollowingTarget = serverOverride.ValueRO.IsFollowingTarget;
                selectedTarget.ValueRW.StoppingDistance = serverOverride.ValueRO.StoppingDistance;

                // Clear path so NavMeshPathfindingSystem recalculates it
                pathComponent.ValueRW.HasPath = false;
                pathComponent.ValueRW.CurrentWaypointIndex = 0;

                UnityEngine.Debug.Log($"[SERVER-OVERRIDE] Worker {entity.Index} moving to position {serverOverride.ValueRO.TargetPosition}, target entity {serverOverride.ValueRO.TargetEntity.Index}, IsFollowing={serverOverride.ValueRO.IsFollowingTarget}");

                // Remove the override component - it's been applied
                state.EntityManager.RemoveComponent<ServerMoveOverrideComponent>(entity);
            }

            if (entityCount > 0)
            {
                UnityEngine.Debug.Log($"[SERVER-OVERRIDE] Processed {entityCount} entities with ServerMoveOverrideComponent");
            }
        }
    }
}

