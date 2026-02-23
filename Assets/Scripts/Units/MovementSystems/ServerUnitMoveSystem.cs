using Units.Worker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.MovementSystems
{
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

            if (count == 0)
            {
                pathComponent.ValueRW.HasPath = false;
                return;
            }

            float3 w0 = waypointsInput.ValueRO.W0;
            bool isNewPath = !pathComponent.ValueRO.HasPath ||
                             math.distancesq(pathComponent.ValueRO.LastTargetPosition, w0) > 0.01f;

            if (isNewPath)
            {
                pathComponent.ValueRW.HasPath = true;
                pathComponent.ValueRW.CurrentWaypointIndex = 0;
                pathComponent.ValueRW.LastTargetPosition = w0;
            }

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
