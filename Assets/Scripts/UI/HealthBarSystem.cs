using Combat;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct HealthBarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<UIPrefabs>();
        }

        public void OnUpdate(ref SystemState state)
        {
            EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton =
                SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((LocalTransform transform, HealthBarOffsetComponent healthBarOffset,
                         MaxHitPointsComponent maxHitPoints, Entity entity) in SystemAPI
                         .Query<LocalTransform, HealthBarOffsetComponent, MaxHitPointsComponent>()
                         .WithNone<HealthBarUIReferenceComponent>()
                         .WithEntityAccess())
            {
                SpawnHealthBar(transform, healthBarOffset, maxHitPoints, ecb, entity);
            }

            foreach ((LocalTransform transform, HealthBarOffsetComponent healthBarOffset,
                         CurrentHitPointsComponent currentHitPoints, MaxHitPointsComponent maxHitPoints,
                         HealthBarUIReferenceComponent healthBarUI) in SystemAPI
                         .Query<LocalTransform, HealthBarOffsetComponent, CurrentHitPointsComponent,
                             MaxHitPointsComponent, HealthBarUIReferenceComponent>())
            {
                UpdateHealthBar(transform, healthBarOffset, healthBarUI, currentHitPoints, maxHitPoints);
            }

            foreach ((UnitSelectionComponent unitSelectionComponent, HealthBarUIReferenceComponent healthBar) in
                     SystemAPI.Query<UnitSelectionComponent, HealthBarUIReferenceComponent>())
            {
                EnableHealthBar(unitSelectionComponent, healthBar);
            }

            foreach ((HealthBarUIReferenceComponent healthBarUI, Entity entity) in SystemAPI
                         .Query<HealthBarUIReferenceComponent>().WithNone<LocalTransform>()
                         .WithEntityAccess())
            {
                CleanupHealthBar(healthBarUI, ecb, entity);
            }
        }

        private void CleanupHealthBar(HealthBarUIReferenceComponent healthBarUI, EntityCommandBuffer ecb, Entity entity)
        {
            Object.Destroy(healthBarUI.Value);
            ecb.RemoveComponent<HealthBarUIReferenceComponent>(entity);
        }

        private void UpdateHealthBar(LocalTransform transform, HealthBarOffsetComponent healthBarOffset,
            HealthBarUIReferenceComponent healthBarUI, CurrentHitPointsComponent currentHitPoints,
            MaxHitPointsComponent maxHitPoints)
        {
            float3 healthBarPosition = transform.Position + healthBarOffset.Value;
            healthBarUI.Value.transform.position = healthBarPosition;
            healthBarUI.Value.UpdateHealthBar(currentHitPoints.Value, maxHitPoints.Value);
        }

        private void SpawnHealthBar(LocalTransform transform, HealthBarOffsetComponent healthBarOffset,
            MaxHitPointsComponent maxHitPoints, EntityCommandBuffer ecb, Entity entity)
        {
            HealthBarView healthBarPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().HealthBar;
            float3 spawnPosition = transform.Position + healthBarOffset.Value;
            HealthBarView newHealthBar = Object.Instantiate(healthBarPrefab, spawnPosition, Quaternion.identity);
            newHealthBar.UpdateHealthBar(maxHitPoints.Value, maxHitPoints.Value);
            ecb.AddComponent(entity, new HealthBarUIReferenceComponent() { Value = newHealthBar });
        }

        private void EnableHealthBar(UnitSelectionComponent unitSelectionComponent,
            HealthBarUIReferenceComponent healthBar)
        {
            HealthBarView barView = healthBar.Value;
            if (unitSelectionComponent.IsSelected)
            {
                barView.Enable();
            }
            else
            {
                barView.Disable();
            }
        }
    }
}