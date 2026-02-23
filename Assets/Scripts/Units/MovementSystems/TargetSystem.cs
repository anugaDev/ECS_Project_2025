// DISABLED: TargetSystem is no longer needed!
// Systems now read directly from SetInputStateTargetComponent (like ChampMoveSystem did)
// This eliminates the intermediate CurrentTargetComponent that was causing synchronization issues

/*using Units;
using Units.Worker;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [BurstCompile]
    public partial struct TargetSystem : ISystem
    {
        private bool _hasNewTarget;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRW<SetInputStateTargetComponent> inputTarget,
                     RefRW<SetServerStateTargetComponent> serverTarget,
                     RefRW<CurrentTargetComponent> currentTarget,
                     RefRW<PathComponent> pathComponent,
                     Entity entity)
                     in SystemAPI.Query<RefRW<SetInputStateTargetComponent>,
                                       RefRW<SetServerStateTargetComponent>,
                                       RefRW<CurrentTargetComponent>,
                                       RefRW<PathComponent>>()
                         .WithAll<UnitTagComponent>()
                         .WithEntityAccess())
            {
                _hasNewTarget = false;

                // TESTING: Disable server target to test with input only
                //SetServerTarget(serverTarget, currentTarget, entity);
                SetInputTarget(inputTarget, currentTarget, entity);
                ResetPath(pathComponent);
            }
        }

        private void ResetPath(RefRW<PathComponent> pathComponent)
        {
            if (!_hasNewTarget)
            {
                return;
            }

            pathComponent.ValueRW.HasPath = false;
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathComponent.ValueRW.LastTargetPosition = float3.zero; // Reset to force pathfinding recalculation
        }

        private void SetInputTarget(RefRW<SetInputStateTargetComponent> inputTarget, RefRW<CurrentTargetComponent> currentTarget, Entity entity)
        {
            if (!inputTarget.ValueRO.HasNewTarget)
            {
                return;
            }

            UnityEngine.Debug.Log($"[TARGET-SYSTEM] Entity {entity.Index}: INPUT setting new target: position = {inputTarget.ValueRO.TargetPosition}");

            SetCurrentTarget(currentTarget, inputTarget.ValueRO.TargetEntity, inputTarget.ValueRO.TargetPosition,
                inputTarget.ValueRO.IsFollowingTarget, inputTarget.ValueRO.StoppingDistance);
            inputTarget.ValueRW.HasNewTarget = false;
            _hasNewTarget = true;
        }

        private void SetServerTarget(RefRW<SetServerStateTargetComponent> serverTarget, RefRW<CurrentTargetComponent> currentTarget, Entity entity)
        {
            if (!serverTarget.ValueRO.HasNewTarget)
            {
                return;
            }

            UnityEngine.Debug.Log($"[TARGET-SYSTEM] Entity {entity.Index}: SERVER setting new target: position = {serverTarget.ValueRO.TargetPosition}");

            SetCurrentTarget(currentTarget, serverTarget.ValueRO.TargetEntity, serverTarget.ValueRO.TargetPosition,
                serverTarget.ValueRO.IsFollowingTarget, serverTarget.ValueRO.StoppingDistance);

            serverTarget.ValueRW.HasNewTarget = false;
            _hasNewTarget = true;
        }

        private void SetCurrentTarget(RefRW<CurrentTargetComponent> currentTarget, Entity valueROTargetEntity,
            float3 valueROTargetPosition, bool valueROIsFollowingTarget, float valueROStoppingDistance)
        {
            bool wasReached = currentTarget.ValueRO.IsTargetReached;

            currentTarget.ValueRW.TargetEntity = valueROTargetEntity;
            currentTarget.ValueRW.TargetPosition = valueROTargetPosition;
            currentTarget.ValueRW.IsFollowingTarget = valueROIsFollowingTarget;
            currentTarget.ValueRW.StoppingDistance = valueROStoppingDistance;
            currentTarget.ValueRW.IsTargetReached = false;

            UnityEngine.Debug.Log($"[TARGET-SYSTEM] Setting new target: IsTargetReached changed from {wasReached} to false, position = {valueROTargetPosition}");
        }
    }
}*/

/*using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace TMG.NFE_Tutorial
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct ChampMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            foreach (var (transform, movePosition, moveSpeed) in SystemAPI
                         .Query<RefRW<LocalTransform>, RefRW<SetInputStateTargetComponent>, UnitMoveSpeedComponent>()
                         .WithAll<Simulate>())
            {
                var moveTarget = movePosition.ValueRW.TargetPosition;
                moveTarget.y = transform.ValueRO.Position.y;

                if(math.distancesq(transform.ValueRO.Position, moveTarget) < 0.001f) continue;
                var moveDirection = math.normalize(moveTarget - transform.ValueRO.Position);
                var moveVector = moveDirection * moveSpeed.Speed * deltaTime;
                transform.ValueRW.Position += moveVector;
                transform.ValueRW.Rotation = quaternion.LookRotationSafe(moveDirection, math.up());
            }
        }
    }
}*/

