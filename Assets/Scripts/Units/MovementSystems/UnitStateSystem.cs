using Units.Worker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

/*namespace Units.MovementSystems
{
    /// <summary>
    /// State machine system that checks if units have reached their target
    /// and sets IsTargetReached flag which triggers worker systems.
    /// Runs in PredictedSimulationSystemGroup on both client and server for proper prediction.
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct UnitStateSystem : ISystem
    {
        private const float REACHED_THRESHOLD_SQ = 0.25f; // 0.5 units squared

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int entityCount = 0;
            foreach ((RefRO<LocalTransform> transform,
                     RefRW<CurrentTargetComponent> currentTarget,
                     RefRO<PathComponent> pathComponent,
                     DynamicBuffer<PathWaypointBuffer> pathBuffer,
                     Entity entity)
                     in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRW<CurrentTargetComponent>,
                                       RefRO<PathComponent>,
                                       DynamicBuffer<PathWaypointBuffer>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                entityCount++;

                // Skip if already reached
                if (currentTarget.ValueRO.IsTargetReached)
                {
                    continue;
                }

                // CRITICAL FIX: Only check if target is reached when unit has a path
                // This prevents marking target as reached before pathfinding completes
                if (!pathComponent.ValueRO.HasPath)
                {
                    continue;
                }

                // CRITICAL FIX 2: Only check if reached when on the LAST waypoint
                // This ensures the unit has actually moved through the path
                int lastWaypointIndex = pathBuffer.Length - 1;
                if (pathComponent.ValueRO.CurrentWaypointIndex < lastWaypointIndex)
                {
                    continue;
                }

                // Check if unit has reached target
                // CRITICAL FIX: Check distance to LAST WAYPOINT, not target position
                // The waypoint has the correct Y coordinate, while target position might have Y=0
                float3 lastWaypoint = pathBuffer[lastWaypointIndex].Position;

                // Ignore Y difference (same as UnitMoveSystem does)
                float3 unitPos = transform.ValueRO.Position;
                float3 waypointPos = lastWaypoint;
                waypointPos.y = unitPos.y;

                float distanceSq = math.distancesq(unitPos, waypointPos);
                float stoppingDistanceSq = currentTarget.ValueRO.StoppingDistance * currentTarget.ValueRO.StoppingDistance;
                float distance = math.sqrt(distanceSq);
                float threshold = math.sqrt(stoppingDistanceSq + REACHED_THRESHOLD_SQ);

                UnityEngine.Debug.Log($"[STATE] Entity {entity.Index}: Checking if reached - Distance={distance:F3}, Threshold={threshold:F3}, StoppingDistance={currentTarget.ValueRO.StoppingDistance:F3}, WaypointIndex={pathComponent.ValueRO.CurrentWaypointIndex}/{pathBuffer.Length}");

                if (distanceSq <= stoppingDistanceSq + REACHED_THRESHOLD_SQ)
                {
                    // Mark target as reached
                    currentTarget.ValueRW.IsTargetReached = true;
                    UnityEngine.Debug.LogWarning($"[STATE] Entity {entity.Index}: *** TARGET REACHED! *** Distance={distance:F3} <= Threshold={threshold:F3}");
                }
            }
        }
    }
}*/

