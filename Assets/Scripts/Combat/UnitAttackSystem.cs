using ElementCommons;
using Units.MovementSystems;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Combat
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct UnitAttackSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        }

        public void OnUpdate(ref SystemState state)
        {
            BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

            state.Dependency = new UnitAttackJob
            {
                CurrentTick = networkTime.ServerTick,
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            }.ScheduleParallel(state.Dependency);
        }
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct UnitAttackJob : IJobEntity
    {
        [ReadOnly] public NetworkTick CurrentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> TransformLookup;
        [BurstCompile]
        private void Execute(ref DynamicBuffer<UnitAttackCooldown> attackCooldown, in UnitAttackProperties attackProperties,
            in UnitTargetEntity targetEntity, Entity UnitEntity, ElementTeamComponent team, [ChunkIndexInQuery] int sortKey)
        {
            if (!TransformLookup.HasComponent(targetEntity.Value)) return;
            if (!attackCooldown.GetDataAtTick(CurrentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.Value = NetworkTick.Invalid;
            }

            bool canAttack = !cooldownExpirationTick.Value.IsValid ||
                             CurrentTick.IsNewerThan(cooldownExpirationTick.Value);
            if (!canAttack) return;

            float3 spawnPosition = TransformLookup[UnitEntity].Position + attackProperties.FirePointOffset;
            float3 targetPosition = TransformLookup[targetEntity.Value].Position;

            LocalTransform newAttackTransform = LocalTransform.FromPositionRotation(spawnPosition,
                quaternion.LookRotationSafe(targetPosition - spawnPosition, math.up()));

            //_entityCommandBuffer.SetComponent(sortKey, newAttack, newAttackTransform);
            //_entityCommandBuffer.SetComponent(sortKey, newAttack, team);

            NetworkTick newCooldownTick = CurrentTick;
            newCooldownTick.Add(attackProperties.CooldownTickCount);
            attackCooldown.AddCommandData(new UnitAttackCooldown { Tick = CurrentTick, Value = newCooldownTick });
        }
    }
}