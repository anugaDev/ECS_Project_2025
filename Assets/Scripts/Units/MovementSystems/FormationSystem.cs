using ElementCommons;
using Units.Worker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

/*namespace Units.MovementSystems
{
    /// <summary>
    /// System that arranges selected units into a grid formation when they receive move commands.
    /// Uses SetInputStateTargetComponent to set formation positions.
    /// Runs before NavMeshPathfindingSystem to adjust targets before pathfinding.
    /// DISABLED FOR TESTING - Testing basic movement only
    /// </summary>
    //[UpdateInGroup(typeof(GhostInputSystemGroup))]
    //[UpdateAfter(typeof(PlayerInputs.UnitMoveInputSystem))]
    //[BurstCompile]
    public partial struct FormationSystem_DISABLED : ISystem
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

            // Collect all units that have received new input targets
            foreach ((RefRO<SetInputStateTargetComponent> inputTarget,
                     RefRO<ElementSelectionComponent> selection,
                     RefRO<ElementTeamComponent> team,
                     Entity entity)
                     in SystemAPI.Query<RefRO<SetInputStateTargetComponent>,
                                       RefRO<ElementSelectionComponent>,
                                       RefRO<ElementTeamComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                if (!inputTarget.ValueRO.HasNewTarget || !selection.ValueRO.IsSelected)
                    continue;

                SetCommandedUnitCandidate(entity, inputTarget, team);
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

            // Get the current input target to preserve entity target and other settings
            SetInputStateTargetComponent currentInput = state.EntityManager.GetComponentData<SetInputStateTargetComponent>(_commandedUnits[unitIndex]);

            // Update only the position with formation offset
            currentInput.TargetPosition = newTarget;
            currentInput.HasNewTarget = true;

            state.EntityManager.SetComponentData(_commandedUnits[unitIndex], currentInput);
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

        private void SetCommandedUnitCandidate(Entity entity, RefRO<SetInputStateTargetComponent> inputTarget, RefRO<ElementTeamComponent> team)
        {
            _commandedUnits.Add(entity);
            _targetPositions.Add(inputTarget.ValueRO.TargetPosition);
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
}*/

