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
        private const float OFFSET_MULTIPLIER = 0.5f;
        
        private const float GRID_SPACING = 3.0f;
        
        private NativeList<Entity> _commandedUnits;

        private NativeList<float3> _targetPositions;

        private NativeList<int> _teams;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTagComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeLists();

            foreach ((RefRO<UnitTargetPositionComponent> targetPos, RefRO<ElementSelectionComponent> selection, RefRO<ElementTeamComponent> team, Entity entity)
                     in SystemAPI.Query<RefRO<UnitTargetPositionComponent>, RefRO<ElementSelectionComponent>, RefRO<ElementTeamComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                if (!targetPos.ValueRO.MustMove || !selection.ValueRO.IsSelected)
                {
                    continue;
                }

                SetCommandedUnitCandidate(entity, targetPos, team);
            }

            if (_commandedUnits.Length == 0)
            {
                ResetLists();
                return;
            }

            NativeHashSet<int> processedIndices = new NativeHashSet<int>(_commandedUnits.Length, Allocator.Temp);

            for (int commandedUnitIndex = 0; commandedUnitIndex < _commandedUnits.Length; commandedUnitIndex++)
            {
                UpdateProcessedUnit(state, processedIndices, commandedUnitIndex);
            }

            processedIndices.Dispose();
            _commandedUnits.Dispose();
            _targetPositions.Dispose();
            _teams.Dispose();
        }

        private void UpdateProcessedUnit(SystemState state, NativeHashSet<int> processedIndices, int commandedUnitIndex)
        {
            if (processedIndices.Contains(commandedUnitIndex))
            {
                return;
            }

            float3 baseUnitTarget = _targetPositions[commandedUnitIndex];
            int unitTeam = _teams[commandedUnitIndex];
            NativeList<int> groupIndices = new NativeList<int>(Allocator.Temp);
            ProcessUnitTeam(processedIndices, unitTeam, baseUnitTarget, groupIndices);
            SetTargets(state, groupIndices, baseUnitTarget);
            groupIndices.Dispose();
        }

        private void ProcessUnitTeam(NativeHashSet<int> processedIndices, int unitTeam, float3 baseUnitTarget,
            NativeList<int> groupIndices)
        {
            for (int processedUnitIndex = 0; processedUnitIndex < _commandedUnits.Length; processedUnitIndex++)
            {
                ProcessUnitTeam(processedIndices, unitTeam, baseUnitTarget, groupIndices, processedUnitIndex);
            }
        }

        private void ProcessUnitTeam(NativeHashSet<int> processedIndices, int unitTeam, float3 baseUnitTarget,
            NativeList<int> groupIndices, int processedUnitIndex)
        {
            if (IsUnitTeam(unitTeam, baseUnitTarget, processedUnitIndex))
            {
                return;
            }

            groupIndices.Add(processedUnitIndex);
            processedIndices.Add(processedUnitIndex);
        }

        private bool IsUnitTeam(int unitTeam, float3 baseUnitTarget, int processedUnitIndex)
        {
            return _teams[processedUnitIndex] != unitTeam ||
                   !(math.distance(_targetPositions[processedUnitIndex], baseUnitTarget) < 1.0f);
        }

        private void SetTargets(SystemState state, NativeList<int> groupIndices, float3 baseUnitTarget)
        {
            if (groupIndices.Length <= 0)
            {
                return;
            }

            SetTargetsPositions(state, groupIndices, baseUnitTarget);
        }

        private void SetTargetsPositions(SystemState state, NativeList<int> groupIndices, float3 baseUnitTarget)
        {
            for (int groupIndex = 0; groupIndex < groupIndices.Length; groupIndex++)
            {
                SetTargetPositionComponent(state, groupIndices, baseUnitTarget, groupIndex);
            }
        }

        private void SetTargetPositionComponent(SystemState state, NativeList<int> groupIndices, float3 baseUnitTarget,
            int groupIndex)
        {
            int unitIndex = groupIndices[groupIndex];
            float3 formationOffset = CalculateGridOffset(groupIndex, groupIndices.Length);
            float3 newTarget = baseUnitTarget + formationOffset;
            state.EntityManager.SetComponentData(_commandedUnits[unitIndex], GetTargetPositionComponent(newTarget));
        }

        private static UnitTargetPositionComponent GetTargetPositionComponent(float3 newTarget)
        {
            return new UnitTargetPositionComponent
            {
                Value = newTarget,
                MustMove = true
            };
        }

        private void InitializeLists()
        {
            _commandedUnits = new NativeList<Entity>(Allocator.Temp);
            _targetPositions = new NativeList<float3>(Allocator.Temp);
            _teams = new NativeList<int>(Allocator.Temp);
        }

        private void ResetLists()
        {
            _commandedUnits.Dispose();
            _targetPositions.Dispose();
            _teams.Dispose();
        }

        private void SetCommandedUnitCandidate(Entity entity, RefRO<UnitTargetPositionComponent> targetPos, RefRO<ElementTeamComponent> team)
        {
            _commandedUnits.Add(entity);
            _targetPositions.Add(targetPos.ValueRO.Value);
            _teams.Add((int)team.ValueRO.Team);
        }

        [BurstCompile]
        private float3 CalculateGridOffset(int index, int totalUnits)
        {
            int columns = (int)math.ceil(math.sqrt(totalUnits));
            int rows = (int)math.ceil(totalUnits / (float)columns);
            int row = index / columns;
            int col = index % columns;
            float offsetX = (col - (columns - 1) * OFFSET_MULTIPLIER) * GRID_SPACING;
            float offsetZ = (row - (rows - 1) * OFFSET_MULTIPLIER) * GRID_SPACING;

            return new float3(offsetX, 0, offsetZ);
        }
    }
}

