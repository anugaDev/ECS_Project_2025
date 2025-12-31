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
            EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            EntityCommandBuffer ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach ((LocalTransform transform, HealthBarOffsetComponent healthBarOffset, MaxHitPointsComponent maxHitPoints, Entity entity) in 
                     SystemAPI.Query<LocalTransform, HealthBarOffsetComponent, MaxHitPointsComponent>().WithNone<HealthBarUIReferenceComponent>().WithEntityAccess())
            {
                SpawnHealthbars(transform, healthBarOffset, maxHitPoints, ecb, entity);
            }
            
            foreach ((LocalTransform transform, HealthBarOffsetComponent healthBarOffset, CurrentHitPointsComponent currentHitPoints, MaxHitPointsComponent maxHitPoints, HealthBarUIReferenceComponent healthBarUI) in 
                     SystemAPI.Query<LocalTransform, HealthBarOffsetComponent, CurrentHitPointsComponent, MaxHitPointsComponent, HealthBarUIReferenceComponent>())
            {
                UpdateHealthBars(transform, healthBarOffset, healthBarUI, currentHitPoints, maxHitPoints);
            }
            
            foreach ((HealthBarUIReferenceComponent healthBarUI, Entity entity) in 
                     SystemAPI.Query<HealthBarUIReferenceComponent>().WithNone<LocalTransform>().WithEntityAccess())
            {
                CleanupHelathBars(healthBarUI, ecb, entity);
            }
        }

        private void SpawnHealthbars(LocalTransform transform, HealthBarOffsetComponent healthBarOffsetComponent,
            MaxHitPointsComponent maxHitPoints, EntityCommandBuffer ecb, Entity entity)
        {
            HealthBarView healthBarPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().HealthBar;
            float3 spawnPosition = transform.Position + healthBarOffsetComponent.Value;
            HealthBarView newHealthBar = Object.Instantiate(healthBarPrefab, spawnPosition, Quaternion.identity);
            newHealthBar.SetHealthBar(maxHitPoints.Value, maxHitPoints.Value);
            ecb.AddComponent(entity, new HealthBarUIReferenceComponent { Value = newHealthBar });
        }

        private void UpdateHealthBars(LocalTransform transform, HealthBarOffsetComponent healthBarOffsetComponent,
            HealthBarUIReferenceComponent healthBarUI, CurrentHitPointsComponent currentHitPoints, MaxHitPointsComponent maxHitPoints)
        {
            float3 healthBarPosition = transform.Position + healthBarOffsetComponent.Value;
            HealthBarView healthBarView = healthBarUI.Value;
            healthBarView.transform.position = healthBarPosition;
            healthBarView.SetHealthBar(currentHitPoints.Value, maxHitPoints.Value);
        }

        private static void CleanupHelathBars(HealthBarUIReferenceComponent healthBarUI, EntityCommandBuffer ecb, Entity entity)
        {
            Object.Destroy(healthBarUI.Value);
            ecb.RemoveComponent<HealthBarUIReferenceComponent>(entity);
        }
    }
}