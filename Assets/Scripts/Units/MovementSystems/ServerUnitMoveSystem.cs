using Units.Worker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.MovementSystems
{
    /// <summary>
    /// SERVER-ONLY counterpart to UnitMoveSystem.
    ///
    /// Reads NavMesh waypoints from UnitWaypointsInputComponent (delivered via NetCode's
    /// command buffer from the client's GhostInputSystemGroup). Uses PathComponent on the
    /// server (now AllPredicted) for monotonic waypoint index tracking.
    ///
    /// MONOTONIC INDEX: Scan starts from the stored CurrentWaypointIndex (never goes
    /// backward). Once a waypoint is considered passed it stays passed, preventing
    /// threshold boundary bouncing when server and client positions differ by a tiny amount.
    /// Server also detects new paths via W0 change (stored in LastTargetPosition).
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ServerUnitMoveSystem : ISystem
    {
        private const float FINAL_POSITION_THRESHOLD = 0.1f;
        private const float WAYPOINT_THRESHOLD = 0.5f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTagComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRW<LocalTransform> transform,
                     RefRO<UnitWaypointsInputComponent> waypointsInput,
                     RefRW<PathComponent> pathComponent,
                     RefRO<UnitMoveSpeedComponent> moveSpeed)
                     in SystemAPI.Query<RefRW<LocalTransform>,
                                       RefRO<UnitWaypointsInputComponent>,
                                       RefRW<PathComponent>,
                                       RefRO<UnitMoveSpeedComponent>>()
                         .WithAll<Simulate, UnitTagComponent>())
            {
                MoveUnit(transform, waypointsInput, pathComponent, moveSpeed, deltaTime);
            }
        }

        [BurstCompile]
        private static void MoveUnit(
            RefRW<LocalTransform> transform,
            RefRO<UnitWaypointsInputComponent> waypointsInput,
            RefRW<PathComponent> pathComponent,
            RefRO<UnitMoveSpeedComponent> moveSpeed,
            float deltaTime)
        {
            int count = waypointsInput.ValueRO.WaypointCount;

            // Client cleared count to 0 after path ended
            if (count == 0)
            {
                pathComponent.ValueRW.HasPath = false;
                return;
            }

            // Detect new path: W0 changed → reset index to 0
            float3 w0 = waypointsInput.ValueRO.W0;
            bool isNewPath = !pathComponent.ValueRO.HasPath ||
                             math.distancesq(pathComponent.ValueRO.LastTargetPosition, w0) > 0.01f;

            if (isNewPath)
            {
                pathComponent.ValueRW.HasPath = true;
                pathComponent.ValueRW.CurrentWaypointIndex = 0;
                pathComponent.ValueRW.LastTargetPosition = w0;
            }

            // Monotonic index — same logic as UnitMoveSystem
            int startIndex = math.clamp(pathComponent.ValueRO.CurrentWaypointIndex, 0, count - 1);
            float3 pos = transform.ValueRO.Position;

            for (int i = startIndex; i < count; i++)
            {
                float3 waypoint = waypointsInput.ValueRO.GetWaypoint(i);
                waypoint.y = pos.y;

                float3 toWaypoint = waypoint - pos;
                toWaypoint.y = 0f;
                float distance = math.length(toWaypoint);

                bool isLast = i == count - 1;
                float threshold = isLast ? FINAL_POSITION_THRESHOLD : WAYPOINT_THRESHOLD;

                if (distance < threshold)
                {
                    if (isLast)
                    {
                        pathComponent.ValueRW.HasPath = false;
                        return;
                    }
                    pathComponent.ValueRW.CurrentWaypointIndex = i + 1;
                    continue;
                }

                if (distance < 0.001f) return;

                float3 dir = toWaypoint / distance;
                float move = moveSpeed.ValueRO.Speed * deltaTime;
                transform.ValueRW.Position += dir * move;
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(dir, math.up());
                return;
            }
        }
    }
}
