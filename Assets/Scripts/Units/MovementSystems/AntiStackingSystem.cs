using ElementCommons;
using PlayerInputs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(Navigation.NavMeshPathfindingSystem))]
    [BurstCompile]
    public partial struct AntiStackingSystem : ISystem
    {
        private const float STACKING_THRESHOLD = 1.5f;
        private const float PUSH_DISTANCE = 3.0f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTagComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            NativeList<Entity> stationaryUnits = new NativeList<Entity>(Allocator.Temp);
            NativeList<float3> stationaryPositions = new NativeList<float3>(Allocator.Temp);
            NativeList<int> stationaryTeams = new NativeList<int>(Allocator.Temp);

            foreach ((RefRO<LocalTransform> transform, RefRO<PathComponent> pathComponent, RefRO<ElementTeamComponent> team, Entity entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathComponent>, RefRO<ElementTeamComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                if (!pathComponent.ValueRO.HasPath)
                {
                    stationaryUnits.Add(entity);
                    stationaryPositions.Add(transform.ValueRO.Position);
                    stationaryTeams.Add((int)team.ValueRO.Team);
                }
            }

            if (stationaryUnits.Length < 2)
            {
                stationaryUnits.Dispose();
                stationaryPositions.Dispose();
                stationaryTeams.Dispose();
                return;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            for (int i = 0; i < stationaryUnits.Length; i++)
            {
                for (int j = i + 1; j < stationaryUnits.Length; j++)
                {
                    if (stationaryTeams[i] != stationaryTeams[j]) continue;

                    float distance = math.distance(stationaryPositions[i], stationaryPositions[j]);

                    if (distance < STACKING_THRESHOLD)
                    {
                        float3 newPosition = FindFreePosition(stationaryPositions[j], stationaryPositions, j, stationaryUnits.Length);

                        ecb.SetComponent(stationaryUnits[j], new UnitTargetPositionComponent
                        {
                            Value = newPosition,
                            MustMove = true
                        });
                    }
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
            stationaryUnits.Dispose();
            stationaryPositions.Dispose();
            stationaryTeams.Dispose();
        }

        [BurstCompile]
        private float3 FindFreePosition(float3 currentPos, NativeList<float3> allPositions, int skipIndex, int totalUnits)
        {
            int attempts = 8;
            float angleStep = 360.0f / attempts;

            for (int i = 0; i < attempts; i++)
            {
                float angle = math.radians(angleStep * i);
                float3 offset = new float3(math.cos(angle) * PUSH_DISTANCE, 0, math.sin(angle) * PUSH_DISTANCE);
                float3 candidatePos = currentPos + offset;
                bool isFree = true;

                for (int j = 0; j < totalUnits; j++)
                {
                    if (j == skipIndex) continue;

                    if (math.distance(candidatePos, allPositions[j]) < STACKING_THRESHOLD)
                    {
                        isFree = false;
                        break;
                    }
                }

                if (isFree)
                {
                    return candidatePos;
                }
            }

            float randomAngle = math.radians(UnityEngine.Random.Range(0f, 360f));
            return currentPos + new float3(math.cos(randomAngle) * PUSH_DISTANCE, 0, math.sin(randomAngle) * PUSH_DISTANCE);
        }
    }
}

