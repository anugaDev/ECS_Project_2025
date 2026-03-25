using Combat;
using ElementCommons;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Units.Worker;

namespace Units.MovementSystems
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(UnitStateSystem))]
    [UpdateBefore(typeof(WorkerActionSystem))]
    public partial class UnitAttackActionSystem : SystemBase
    {
        private const float DEFAULT_ATTACK_RANGE = 4.0f;

        private ComponentLookup<CurrentHitPointsComponent> _hpLookup;
        private ComponentLookup<ElementTeamComponent>      _teamLookup;
        private ComponentLookup<LocalTransform>            _transformLookup;
        private ComponentLookup<UnitAttackRange>           _attackRangeLookup;

        protected override void OnCreate()
        {
            _hpLookup          = GetComponentLookup<CurrentHitPointsComponent>(true);
            _teamLookup        = GetComponentLookup<ElementTeamComponent>(true);
            _transformLookup   = GetComponentLookup<LocalTransform>(true);
            _attackRangeLookup = GetComponentLookup<UnitAttackRange>(true);
            RequireForUpdate<UnitTagComponent>();
        }

        protected override void OnUpdate()
        {
            _hpLookup.Update(this);
            _teamLookup.Update(this);
            _transformLookup.Update(this);
            _attackRangeLookup.Update(this);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            // First loop: new player input cancels any ongoing attack
            foreach ((RefRO<SetInputStateTargetComponent>  inputTarget,
                      RefRW<SetServerStateTargetComponent> serverTarget,
                      Entity entity) in SystemAPI.Query<RefRO<SetInputStateTargetComponent>,
                                                        RefRW<SetServerStateTargetComponent>>()
                         .WithAll<UnitTagComponent, Simulate, UnitAttackingTagComponent>().WithEntityAccess())
            {
                if (inputTarget.ValueRO.TargetVersion <= serverTarget.ValueRO.TargetVersion)
                    continue;

                // Remove the old attack tag. Do NOT update serverTarget.TargetVersion here —
                // the second loop must process this same command next frame (once the ECB
                // has physically removed the tag and WithNone<UnitAttackingTagComponent>
                // will match the entity again). Consuming the version here would silently
                // drop the incoming attack command.
                ecb.RemoveComponent<UnitAttackingTagComponent>(entity);
            }

            // Second loop: evaluate whether to start an attack
            foreach ((RefRO<UnitStateComponent>            unitState,
                      RefRO<SetInputStateTargetComponent>  inputTarget,
                      RefRW<SetServerStateTargetComponent> serverTarget,
                      RefRO<ElementTeamComponent>          unitTeam,
                      RefRO<LocalTransform>                unitTransform,
                      Entity entity) in SystemAPI.Query<RefRO<UnitStateComponent>,
                                                       RefRO<SetInputStateTargetComponent>,
                                                       RefRW<SetServerStateTargetComponent>,
                                                       RefRO<ElementTeamComponent>,
                                                       RefRO<LocalTransform>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithNone<UnitAttackingTagComponent>()
                         .WithEntityAccess())
            {
                if (inputTarget.ValueRO.TargetVersion <= serverTarget.ValueRO.TargetVersion)
                    continue;

                Entity target = inputTarget.ValueRO.TargetEntity;

                if (target == Entity.Null || !EntityManager.Exists(target))
                {
                    serverTarget.ValueRW.TargetVersion = inputTarget.ValueRO.TargetVersion;
                    continue;
                }

                if (!_hpLookup.HasComponent(target) || _hpLookup[target].Value <= 0)
                {
                    serverTarget.ValueRW.TargetVersion = inputTarget.ValueRO.TargetVersion;
                    continue;
                }

                if (!_teamLookup.TryGetComponent(target, out ElementTeamComponent targetTeam) ||
                    targetTeam.Team == unitTeam.ValueRO.Team)
                {
                    serverTarget.ValueRW.TargetVersion = inputTarget.ValueRO.TargetVersion;
                    continue;
                }

                // Check whether the target is already within attack range right now.
                // If so, start attacking immediately (even if the unit is still moving).
                // If not, require the unit to be Idle first (UnitAttackSystem will close
                // the distance while UnitAttackingTagComponent is active).
                bool targetInRange = false;
                if (_transformLookup.TryGetComponent(target, out LocalTransform targetTransform))
                {
                    float attackRange = _attackRangeLookup.TryGetComponent(entity, out UnitAttackRange rangeComp)
                        ? rangeComp.Value : DEFAULT_ATTACK_RANGE;

                    float3 toTarget  = targetTransform.Position - unitTransform.ValueRO.Position;
                    toTarget.y = 0f;
                    targetInRange = math.lengthsq(toTarget) <= attackRange * attackRange;
                }

                if (!targetInRange && unitState.ValueRO.State != UnitState.Idle)
                    continue;

                ecb.AddComponent(entity, new UnitAttackingTagComponent { TargetEntity = target });

                // If the unit was moving and the target is already in range, clear its
                // current path so it stops in place and attacks immediately.
                // (The client's NavMeshPathfindingSystem will zero out waypoints the next
                // tick when it sees HasPath=false, stopping ServerUnitMoveSystem.)
                if (targetInRange && unitState.ValueRO.State == UnitState.Moving)
                {
                    ecb.SetComponent(entity, new PathComponent
                    {
                        HasPath              = false,
                        CurrentWaypointIndex = 0,
                        LastTargetPosition   = float3.zero,
                        LastTargetEntity     = Entity.Null
                    });
                }

                serverTarget.ValueRW.TargetVersion = inputTarget.ValueRO.TargetVersion;
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }
    }
}
