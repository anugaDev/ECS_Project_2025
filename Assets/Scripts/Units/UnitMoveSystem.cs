using PlayerInputs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct UnitMoveSystem :ISystem
    {
        private const float POSITION_THRESHOLD = 0.001f;
        
        private float _currentDeltaTime;

        private UnitSelectedComponent _selection;

        public void OnUpdate(ref SystemState state)
        {
            _currentDeltaTime = SystemAPI.Time.DeltaTime;
            foreach ((RefRW<LocalTransform> transform, UnitTargetPositionComponent movePosition, UnitMoveSpeedComponent moveSpeed, UnitSelectedComponent selection) 
                     in SystemAPI.Query<RefRW<LocalTransform>, UnitTargetPositionComponent, UnitMoveSpeedComponent, UnitSelectedComponent>().WithAll<Simulate>())
            {
                float3 moveTarget = movePosition.Value;
                moveTarget.y = transform.ValueRO.Position.y;
                _selection = selection;
                UpdateUnitTargetPosition(transform, moveTarget, moveSpeed);
            }
        }

        private void UpdateUnitTargetPosition(RefRW<LocalTransform> transform, float3 moveTarget, UnitMoveSpeedComponent moveSpeed)
        {
            if (math.distancesq(transform.ValueRO.Position, moveTarget) < POSITION_THRESHOLD && _selection.Selected)
            {
                return;
            }

            SetUnitTargetPosition(transform, moveTarget, moveSpeed);
        }

        private void SetUnitTargetPosition(RefRW<LocalTransform> transform, float3 moveTarget, UnitMoveSpeedComponent moveSpeed)
        {
            float3 moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
            float3 moveVector = moveDirection * moveSpeed.Speed * _currentDeltaTime;
            transform.ValueRW.Position += moveVector;
            transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
        }
    }
}