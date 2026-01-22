using ElementCommons;
using PlayerInputs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateBefore(typeof(Navigation.NavMeshPathfindingSystem))]
    [BurstCompile]
    public partial struct FormationSystem : ISystem
    {
        private const float GRID_SPACING = 3.0f;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTagComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            NativeList<Entity> commandedUnits = new NativeList<Entity>(Allocator.Temp);
            NativeList<float3> targetPositions = new NativeList<float3>(Allocator.Temp);
            NativeList<int> teams = new NativeList<int>(Allocator.Temp);

            foreach ((RefRO<UnitTargetPositionComponent> targetPos, RefRO<ElementSelectionComponent> selection, RefRO<ElementTeamComponent> team, Entity entity)
                     in SystemAPI.Query<RefRO<UnitTargetPositionComponent>, RefRO<ElementSelectionComponent>, RefRO<ElementTeamComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                if (targetPos.ValueRO.MustMove && selection.ValueRO.IsSelected)
                {
                    commandedUnits.Add(entity);
                    targetPositions.Add(targetPos.ValueRO.Value);
                    teams.Add((int)team.ValueRO.Team);
                }
            }

            if (commandedUnits.Length == 0)
            {
                commandedUnits.Dispose();
                targetPositions.Dispose();
                teams.Dispose();
                return;
            }

            NativeHashSet<int> processedIndices = new NativeHashSet<int>(commandedUnits.Length, Allocator.Temp);

            for (int i = 0; i < commandedUnits.Length; i++)
            {
                if (processedIndices.Contains(i)) continue;

                float3 baseTarget = targetPositions[i];
                int myTeam = teams[i];

                NativeList<int> groupIndices = new NativeList<int>(Allocator.Temp);

                for (int j = 0; j < commandedUnits.Length; j++)
                {
                    if (teams[j] == myTeam && math.distance(targetPositions[j], baseTarget) < 1.0f)
                    {
                        groupIndices.Add(j);
                        processedIndices.Add(j);
                    }
                }

                if (groupIndices.Length > 0)
                {
                    for (int k = 0; k < groupIndices.Length; k++)
                    {
                        int unitIndex = groupIndices[k];
                        float3 formationOffset = CalculateGridOffset(k, groupIndices.Length);
                        float3 newTarget = baseTarget + formationOffset;

                        state.EntityManager.SetComponentData(commandedUnits[unitIndex], new UnitTargetPositionComponent
                        {
                            Value = newTarget,
                            MustMove = true
                        });
                    }
                }

                groupIndices.Dispose();
            }

            processedIndices.Dispose();
            commandedUnits.Dispose();
            targetPositions.Dispose();
            teams.Dispose();
        }

        [BurstCompile]
        private float3 CalculateGridOffset(int index, int totalUnits)
        {
            int columns = (int)math.ceil(math.sqrt(totalUnits));
            int rows = (int)math.ceil(totalUnits / (float)columns);
            int row = index / columns;
            int col = index % columns;
            float offsetX = (col - (columns - 1) * 0.5f) * GRID_SPACING;
            float offsetZ = (row - (rows - 1) * 0.5f) * GRID_SPACING;

            return new float3(offsetX, 0, offsetZ);
        }
    }
}

