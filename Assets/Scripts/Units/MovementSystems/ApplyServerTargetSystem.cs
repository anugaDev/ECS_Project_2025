using Units.Worker;
using Unity.Entities;
using Unity.NetCode;

namespace Units.MovementSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(GhostInputSystemGroup))]
    [UpdateAfter(typeof(PlayerInputs.UnitMoveInputSystem))]
    [UpdateBefore(typeof(Navigation.NavMeshPathfindingSystem))]
    public partial class ApplyServerTargetSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<UnitTagComponent>();
        }

        protected override void OnUpdate()
        {
            foreach ((RefRO<SetServerStateTargetComponent>  serverTarget,
                      RefRW<SetInputStateTargetComponent>   inputTarget)
                     in SystemAPI.Query<RefRO<SetServerStateTargetComponent>,
                                        RefRW<SetInputStateTargetComponent>>()
                         .WithAll<UnitTagComponent>())
            {
                // Only apply server target when server version is strictly newer.
                // Player clicks bump the input version higher, so they won't be overwritten.
                if (serverTarget.ValueRO.TargetVersion <= inputTarget.ValueRO.TargetVersion)
                    continue;

                inputTarget.ValueRW.TargetEntity      = serverTarget.ValueRO.TargetEntity;
                inputTarget.ValueRW.TargetPosition    = serverTarget.ValueRO.TargetPosition;
                inputTarget.ValueRW.IsFollowingTarget  = serverTarget.ValueRO.IsFollowingTarget;
                inputTarget.ValueRW.StoppingDistance   = serverTarget.ValueRO.StoppingDistance;
                inputTarget.ValueRW.HasNewTarget       = true;
                inputTarget.ValueRW.TargetVersion      = serverTarget.ValueRO.TargetVersion;
            }
        }
    }
}
