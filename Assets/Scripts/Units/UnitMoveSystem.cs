using ElementCommons;
using PlayerInputs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct UnitMoveSystem : ISystem
    {
        private const float POSITION_THRESHOLD = 0.01f;
        
        private float _currentDeltaTime;

        public void OnUpdate(ref SystemState state)
        {
            _currentDeltaTime = SystemAPI.Time.DeltaTime;
            foreach ((RefRW<LocalTransform> transform, UnitTargetPositionComponent movePosition, UnitMoveSpeedComponent moveSpeed, ElementSelectionComponent selection) 
                     in SystemAPI.Query<RefRW<LocalTransform>, UnitTargetPositionComponent, UnitMoveSpeedComponent, ElementSelectionComponent>().WithAll<Simulate>())
            { 
                if (!movePosition.MustMove)
                {
                    continue;
                }

                UpdateUnitTargetPosition(transform, movePosition, moveSpeed);
            }
        }

        private void UpdateUnitTargetPosition(RefRW<LocalTransform> transform, UnitTargetPositionComponent targetPosition, UnitMoveSpeedComponent moveSpeed)
        {
            float3 moveTarget = targetPosition.Value;
            moveTarget.y = transform.ValueRO.Position.y;
            float targetDistance = math.distancesq(transform.ValueRO.Position, moveTarget);

            if (targetDistance < POSITION_THRESHOLD)
            {
                targetPosition.MustMove = false;
                return;
            }

            MoveUnitTowardsTargetPosition(transform, moveTarget, moveSpeed);
        }

        private void MoveUnitTowardsTargetPosition(RefRW<LocalTransform> transform, float3 moveTarget, UnitMoveSpeedComponent moveSpeed)
        {
            float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
            float3 moveVector = moveDirection * moveSpeed.Speed * _currentDeltaTime;
            transform.ValueRW.Position += moveVector;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
        }
    }
}