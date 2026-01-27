using ElementCommons;
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
        private const float FINAL_POSITION_THRESHOLD = 0.1f;

        private const float WAYPOINT_THRESHOLD = 0.5f;
        
        private RefRW<UnitTargetPositionComponent> _currentTargetPositionComponent;

        private DynamicBuffer<PathWaypointBuffer> _currentPathBuffer;
        
        private RefRO<UnitMoveSpeedComponent> _currentMoveSpeed;
        
        private RefRW<PathComponent> _currentPathComponent;
        
        private RefRW<LocalTransform> _currentTransform;
        
        private float3 _desiredDirection;
        
        private float3 _desiredVelocity;
        
        private float _currentDeltaTime;

        public void OnUpdate(ref SystemState state)
        {
            _currentDeltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRW<LocalTransform> transform, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRO<UnitMoveSpeedComponent> moveSpeed, RefRO<ElementSelectionComponent> selection)
                     in SystemAPI.Query<RefRW<LocalTransform>, RefRW<UnitTargetPositionComponent>,
                         RefRW<PathComponent>, DynamicBuffer<PathWaypointBuffer>,
                         RefRO<UnitMoveSpeedComponent>, RefRO<ElementSelectionComponent>>()
                         .WithAll<Simulate, UnitTagComponent>())
            {
                _currentTargetPositionComponent = targetPosition;
                _currentPathComponent = pathComponent;
                _currentPathBuffer = pathBuffer;
                _currentTransform = transform;
                _currentMoveSpeed = moveSpeed;
                SetUnitPosition(selection.ValueRO.IsSelected);
            }
        }

        private void SetUnitPosition(bool isSelected)
        {
            if (!_currentTargetPositionComponent.ValueRO.MustMove || !_currentPathComponent.ValueRO.HasPath)
            {
                return;
            }

            if (_currentPathComponent.ValueRO.CurrentWaypointIndex >= _currentPathBuffer.Length)
            {
                _currentTargetPositionComponent.ValueRW.MustMove = false;
                _currentPathComponent.ValueRW.HasPath = false;
                return;
            }

            UpdateUnitPosition();
        }

        private void UpdateUnitPosition()
        {
            float3 currentWaypoint = _currentPathBuffer[_currentPathComponent.ValueRO.CurrentWaypointIndex].Position;
            currentWaypoint.y = _currentTransform.ValueRO.Position.y;
            float distanceToWaypoint = math.distance(_currentTransform.ValueRO.Position, currentWaypoint);
            bool isLastWaypoint = _currentPathComponent.ValueRO.CurrentWaypointIndex == _currentPathBuffer.Length - 1;
            float threshold = isLastWaypoint ? FINAL_POSITION_THRESHOLD : WAYPOINT_THRESHOLD;

            if (distanceToWaypoint < threshold)
            {
                if (isLastWaypoint)
                {
                    _currentTargetPositionComponent.ValueRW.MustMove = false;
                    _currentPathComponent.ValueRW.HasPath = false;
                    return;
                }

                _currentPathComponent.ValueRW.CurrentWaypointIndex++;
                return;
            }

            float3 toWaypoint = currentWaypoint - _currentTransform.ValueRO.Position;
            toWaypoint.y = 0;
            SetUnitMovement(toWaypoint);
        }

        private void SetUnitMovement(float3 toWaypoint)
        {
            if (math.lengthsq(toWaypoint) < 0.001f)
            {
                return;
            }

            _desiredDirection = math.normalize(toWaypoint);
            _desiredVelocity = _desiredDirection * _currentMoveSpeed.ValueRO.Speed;
            SetTargetPosition();
            SetRotation();
        }

        private void SetTargetPosition()
        {
            float3 newPosition = _currentTransform.ValueRO.Position;
            newPosition.x += _desiredVelocity.x * _currentDeltaTime;
            newPosition.z += _desiredVelocity.z * _currentDeltaTime;
            _currentTransform.ValueRW.Position = newPosition;
        }

        private void SetRotation()
        {
            if (!IsRotationNeeded())
            {
                return;
            }

            _currentTransform.ValueRW.Rotation = quaternion.LookRotationSafe(_desiredDirection, math.up());
        }

        private bool IsRotationNeeded()
        {
            return (math.lengthsq(_desiredVelocity) > 0.01f);
        }
    }
}