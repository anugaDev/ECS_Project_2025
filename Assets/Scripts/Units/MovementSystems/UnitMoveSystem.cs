using PlayerInputs;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(Navigation.NavMeshPathfindingSystem))]
    [BurstCompile]
    public partial struct UnitMoveSystem : ISystem
    {
        private const float WAYPOINT_THRESHOLD = 0.5f;
        private const float FINAL_POSITION_THRESHOLD = 0.1f;
        private const float MAX_SPEED_MULTIPLIER = 1.5f;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRW<LocalTransform> transform, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRW<VelocityComponent> velocity, RefRO<UnitMoveSpeedComponent> moveSpeed)
                     in SystemAPI.Query<RefRW<LocalTransform>, RefRW<UnitTargetPositionComponent>,
                         RefRW<PathComponent>, DynamicBuffer<PathWaypointBuffer>, RefRW<VelocityComponent>,
                         RefRO<UnitMoveSpeedComponent>>()
                         .WithAll<Simulate, UnitTagComponent>())
            {
                if (!targetPosition.ValueRO.MustMove || !pathComponent.ValueRO.HasPath)
                {
                    velocity.ValueRW.Value = float3.zero;
                    continue;
                }

                if (pathComponent.ValueRO.CurrentWaypointIndex >= pathBuffer.Length)
                {
                    targetPosition.ValueRW.MustMove = false;
                    pathComponent.ValueRW.HasPath = false;
                    velocity.ValueRW.Value = float3.zero;
                    continue;
                }

                float3 currentWaypoint = pathBuffer[pathComponent.ValueRO.CurrentWaypointIndex].Position;
                currentWaypoint.y = transform.ValueRO.Position.y;
                float distanceToWaypoint = math.distance(transform.ValueRO.Position, currentWaypoint);
                bool isLastWaypoint = pathComponent.ValueRO.CurrentWaypointIndex == pathBuffer.Length - 1;
                float threshold = isLastWaypoint ? FINAL_POSITION_THRESHOLD : WAYPOINT_THRESHOLD;

                if (distanceToWaypoint < threshold)
                {
                    if (isLastWaypoint)
                    {
                        targetPosition.ValueRW.MustMove = false;
                        pathComponent.ValueRW.HasPath = false;
                        velocity.ValueRW.Value = float3.zero;
                        continue;
                    }
                    else
                    {
                        pathComponent.ValueRW.CurrentWaypointIndex++;
                        continue;
                    }
                }

                float3 toWaypoint = currentWaypoint - transform.ValueRO.Position;
                toWaypoint.y = 0;

                if (math.lengthsq(toWaypoint) < 0.001f)
                {
                    velocity.ValueRW.Value = float3.zero;
                    continue;
                }

                float3 desiredDirection = math.normalize(toWaypoint);
                float3 desiredVelocity = desiredDirection * moveSpeed.ValueRO.Speed;

                float3 newPosition = transform.ValueRO.Position;
                newPosition.x += desiredVelocity.x * deltaTime;
                newPosition.z += desiredVelocity.z * deltaTime;
                transform.ValueRW.Position = newPosition;
                velocity.ValueRW.Value = desiredVelocity;

                if (math.lengthsq(desiredVelocity) > 0.01f)
                {
                    transform.ValueRW.Rotation = quaternion.LookRotationSafe(desiredDirection, math.up());
                }
            }
        }
    }
}