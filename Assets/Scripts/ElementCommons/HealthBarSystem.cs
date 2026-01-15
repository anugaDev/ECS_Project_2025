using Combat;
using ElementCommons;
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

            foreach ((ElementSelectionComponent elementSelectionComponent, HealthBarUIReferenceComponent healthBar) in
                     SystemAPI.Query<ElementSelectionComponent, HealthBarUIReferenceComponent>())
            {
                if (!elementSelectionComponent.MustEnableFeedback)
                {
                    continue;
                }

                EnableHealthBar(elementSelectionComponent, healthBar);
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
            UnitUIController unitUIPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().UnitUI;
            float3 spawnPosition = transform.Position + healthBarOffset.Value;
            UnitUIController newUnitUI = Object.Instantiate(unitUIPrefab, spawnPosition, Quaternion.identity);
            newUnitUI.UpdateHealthBar(maxHitPoints.Value, maxHitPoints.Value);
            ecb.AddComponent(entity, new HealthBarUIReferenceComponent() { Value = newUnitUI });
        }

        private void EnableHealthBar(ElementSelectionComponent elementSelectionComponent,
            HealthBarUIReferenceComponent healthBar)
        {
            elementSelectionComponent.MustEnableFeedback = false;
            UnitUIController barController = healthBar.Value;

            if (elementSelectionComponent.IsSelected)
            {
                barController.EnableUI();
            }
            else
            {
                barController.DisableUI();
            }
        }
    }
}