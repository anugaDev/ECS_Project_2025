using ElementCommons;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Combat
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSimulationGroup))]
    [UpdateBefore(typeof(ExportPhysicsWorld))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class TowerAttackSystem : SystemBase
    {
        private ComponentLookup<CurrentHitPointsComponent> _hpLookup;
        private ComponentLookup<LocalTransform>        _transformLookup;
        private ComponentLookup<ElementTeamComponent>  _teamLookup;
        private BufferLookup<DamageBufferElement>      _damageBufferLookup;
        private CollisionFilter _attackFilter;

        protected override void OnCreate()
        {
            _hpLookup = GetComponentLookup<CurrentHitPointsComponent>(true);
            _transformLookup = GetComponentLookup<LocalTransform>(true);
            _teamLookup = GetComponentLookup<ElementTeamComponent>(true);
            _damageBufferLookup = GetBufferLookup<DamageBufferElement>(false);
            
            _attackFilter = new CollisionFilter
            {
                BelongsTo = 1 << 6, // Unit Attack Collider
                CollidesWith = 1 << 1 | 1 << 2 | 1 << 4 // Match target layers from UnitTargetingSystem
            };
            
            RequireForUpdate<PhysicsWorldSingleton>();
            RequireForUpdate<TowerAttackProperties>();
        }

        protected override void OnUpdate()
        {
            CompleteDependency();

            _hpLookup.Update(this);
            _transformLookup.Update(this);
            _teamLookup.Update(this);
            _damageBufferLookup.Update(this);

            float deltaTime = SystemAPI.Time.DeltaTime;
            CollisionWorld collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            foreach ((RefRW<TowerAttackProperties> towerPropsRW, RefRO<LocalTransform> towerTransform, RefRO<ElementTeamComponent> towerTeam) in
                     SystemAPI.Query<RefRW<TowerAttackProperties>, RefRO<LocalTransform>, RefRO<ElementTeamComponent>>()
                         .WithAll<Simulate>())
            {
                ref TowerAttackProperties towerProps = ref towerPropsRW.ValueRW;
                float rangeSq = towerProps.AttackRange * towerProps.AttackRange;

                // Check if current target is valid and in range
                bool keepCurrentTarget = false;
                if (towerProps.TargetEntity != Entity.Null && _hpLookup.HasComponent(towerProps.TargetEntity))
                {
                    if (_hpLookup[towerProps.TargetEntity].Value > 0)
                    {
                        if (_transformLookup.HasComponent(towerProps.TargetEntity))
                        {
                            float3 targetPos = _transformLookup[towerProps.TargetEntity].Position;
                            float3 targetPosFlat = new float3(targetPos.x, 0f, targetPos.z);
                            float3 towerPosFlat = new float3(towerTransform.ValueRO.Position.x, 0f, towerTransform.ValueRO.Position.z);
                            float distanceSq = math.distancesq(towerPosFlat, targetPosFlat);
                            if (distanceSq <= rangeSq)
                            {
                                keepCurrentTarget = true;
                            }
                        }
                    }
                }

                if (!keepCurrentTarget)
                {
                    towerProps.TargetEntity = Entity.Null;

                    NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);

                    if (collisionWorld.OverlapSphere(towerTransform.ValueRO.Position, towerProps.AttackRange, ref hits, _attackFilter))
                    {
                        float closestDistSq = rangeSq;
                        Entity bestTarget = Entity.Null;

                        foreach (DistanceHit hit in hits)
                        {
                            if (!_teamLookup.HasComponent(hit.Entity)) continue;
                            if (_teamLookup[hit.Entity].Team == towerTeam.ValueRO.Team) continue;
                            
                            if (_hpLookup.HasComponent(hit.Entity) && _hpLookup[hit.Entity].Value <= 0) continue;

                            float3 enemyPos = _transformLookup.HasComponent(hit.Entity) 
                                ? _transformLookup[hit.Entity].Position 
                                : hit.Position;

                            float3 towerPosFlat = new float3(towerTransform.ValueRO.Position.x, 0f, towerTransform.ValueRO.Position.z);
                            float3 enemyPosFlat = new float3(enemyPos.x, 0f, enemyPos.z);

                            float distSq = math.distancesq(towerPosFlat, enemyPosFlat);
                            if (distSq <= closestDistSq)
                            {
                                closestDistSq = distSq;
                                bestTarget = hit.Entity;
                            }
                        }

                        towerProps.TargetEntity = bestTarget;
                    }

                    hits.Dispose();
                }

                // Deal Damage
                if (towerProps.TargetEntity != Entity.Null)
                {
                    if (_damageBufferLookup.HasBuffer(towerProps.TargetEntity))
                    {
                        int tickDamage = UnityEngine.Mathf.RoundToInt(towerProps.DamagePerSecond * deltaTime);
                        if (tickDamage <= 0) tickDamage = 1;

                        _damageBufferLookup[towerProps.TargetEntity].Add(new DamageBufferElement { Value = tickDamage });
                    }
                }
            }
        }
    }
}
