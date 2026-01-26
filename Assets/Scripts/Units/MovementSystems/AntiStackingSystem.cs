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

        private EntityCommandBuffer _entityCommandBuffer;

        private NativeList<float3> _stationaryPositions;

        private NativeList<Entity> _stationaryUnits;

        private NativeList<int> _stationaryTeams;

        private bool _isPositionFree;

        private float3 _candidatePos;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<UnitTagComponent>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            InitializeLists();

            foreach ((RefRO<LocalTransform> transform, RefRO<PathComponent> pathComponent, RefRO<ElementTeamComponent> team, Entity entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRO<PathComponent>, RefRO<ElementTeamComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                AddUnitToList(pathComponent, entity, transform, team);
            }

            if (_stationaryUnits.Length < 2)
            {
                _stationaryUnits.Dispose();
                _stationaryPositions.Dispose();
                _stationaryTeams.Dispose();
                return;
            }

            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            SetStationaryUnitsTargetPosition();
            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
            DisposeLists();
        }

        private void AddUnitToList(RefRO<PathComponent> pathComponent, Entity entity, RefRO<LocalTransform> transform, RefRO<ElementTeamComponent> team)
        {
            if (pathComponent.ValueRO.HasPath)
            {
                return;
            }

            _stationaryUnits.Add(entity);
            _stationaryPositions.Add(transform.ValueRO.Position);
            _stationaryTeams.Add((int)team.ValueRO.Team);
        }

        private void InitializeLists()
        {
            _stationaryUnits = new NativeList<Entity>(Allocator.Temp);
            _stationaryPositions = new NativeList<float3>(Allocator.Temp);
            _stationaryTeams = new NativeList<int>(Allocator.Temp);
        }

        private void DisposeLists()
        {
            _stationaryUnits.Dispose();
            _stationaryPositions.Dispose();
            _stationaryTeams.Dispose();
        }

        private void SetStationaryUnitsTargetPosition()
        {
            for (int unitIndexRow = 0; unitIndexRow < _stationaryUnits.Length; unitIndexRow++)
            {
                CheckUnitRow(unitIndexRow);
            }
        }

        private void CheckUnitRow(int unitIndexRow)
        {
            for (int unitIndexColumn = unitIndexRow + 1; unitIndexColumn < _stationaryUnits.Length; unitIndexColumn++)
            {
                CheckUnitColumn(unitIndexRow, unitIndexColumn);
            }
        }

        private void CheckUnitColumn(int unitIndexRow, int unitIndexColumn)
        {
            if (_stationaryTeams[unitIndexRow] != _stationaryTeams[unitIndexColumn])
            { 
                return;
            }

            SetStationaryUnitTargetPosition(unitIndexRow, unitIndexColumn);
        }

        private void SetStationaryUnitTargetPosition(int unitIndexRow, int unitIndexColumn)
        {
            float distance = math.distance(_stationaryPositions[unitIndexRow], _stationaryPositions[unitIndexColumn]);

            if (!(distance < STACKING_THRESHOLD))
            {
                return;
            }

            SetTargetPosition(unitIndexColumn);
        }

        private void SetTargetPosition(int unitIndexColumn)
        {
            float3 newPosition = FindFreePosition(_stationaryPositions[unitIndexColumn], _stationaryPositions, unitIndexColumn, _stationaryUnits.Length);
            _entityCommandBuffer.SetComponent(_stationaryUnits[unitIndexColumn], GetTargetPositionComponent(newPosition));
        }

        private static UnitTargetPositionComponent GetTargetPositionComponent(float3 newPosition)
        {
            return new UnitTargetPositionComponent
            {
                Value = newPosition,
                MustMove = true
            };
        }

        [BurstCompile]
        private float3 FindFreePosition(float3 currentPos, NativeList<float3> allPositions, int skipIndex, int totalUnits)
        {
            int attempts = 8;
            float angleStep = 360.0f / attempts;

            for (int attemptIndex = 0; attemptIndex < attempts; attemptIndex++)
            {
                SetFindFreePositionAttempt(currentPos, allPositions, skipIndex, totalUnits, angleStep, attemptIndex);

                if (_isPositionFree)
                {
                    return _candidatePos;
                }
            }

            float randomAngle = math.radians(UnityEngine.Random.Range(0f, 360f));
            return currentPos + new float3(math.cos(randomAngle) * PUSH_DISTANCE, 0, math.sin(randomAngle) * PUSH_DISTANCE);
        }

        private void SetFindFreePositionAttempt(float3 currentPos, NativeList<float3> allPositions, int skipIndex, int totalUnits,
            float angleStep, int attemptIndex)
        {
            SetCandidatePosition(currentPos, angleStep, attemptIndex);
            for (int unitIndex = 0; unitIndex < totalUnits; unitIndex++)
            {
                if (unitIndex == skipIndex)
                {
                    continue;
                }

                if (!IsPositionAllowed(allPositions, unitIndex))
                {
                    continue;
                }

                _isPositionFree = false;
                break;
            }
        }

        private void SetCandidatePosition(float3 currentPos, float angleStep, int attemptIndex)
        {
            float angle = math.radians(angleStep * attemptIndex);
            float3 offset = new float3(math.cos(angle) * PUSH_DISTANCE, 0, math.sin(angle) * PUSH_DISTANCE);
            _candidatePos = currentPos + offset;
            _isPositionFree = true;
        }

        private bool IsPositionAllowed(NativeList<float3> allPositions, int unitIndex)
        {
            return (math.distance(_candidatePos, allPositions[unitIndex]) < STACKING_THRESHOLD);
        }
    }
}

