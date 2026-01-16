using Combat;
using ElementCommons;
using Types;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using RaycastHit = Unity.Physics.RaycastHit;

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
            PhysicsWorldSingleton physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

            foreach ((LocalTransform transform, SelectionFeedbackOffset healthBarOffset,
                         MaxHitPointsComponent maxHitPoints, ElementTeamComponent team, RefRO<PhysicsCollider> collider, Entity entity) in SystemAPI
                         .Query<LocalTransform, SelectionFeedbackOffset, MaxHitPointsComponent, ElementTeamComponent, RefRO<PhysicsCollider>>()
                         .WithNone<HealthBarUIReferenceComponent>()
                         .WithEntityAccess())
            {
                SpawnHealthBar(transform, healthBarOffset, maxHitPoints, ecb, entity, team, collider.ValueRO, physicsWorld);
            }

            foreach ((LocalTransform transform, SelectionFeedbackOffset healthBarOffset,
                         CurrentHitPointsComponent currentHitPoints, MaxHitPointsComponent maxHitPoints,
                         HealthBarUIReferenceComponent healthBarUI) in SystemAPI
                         .Query<LocalTransform, SelectionFeedbackOffset, CurrentHitPointsComponent,
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

        private void UpdateHealthBar(LocalTransform transform, SelectionFeedbackOffset selectionFeedbackOffset,
            HealthBarUIReferenceComponent healthBarUI, CurrentHitPointsComponent currentHitPoints,
            MaxHitPointsComponent maxHitPoints)
        {
            healthBarUI.Value.transform.position = transform.Position;
            healthBarUI.Value.UpdateHealthBar(currentHitPoints.Value, maxHitPoints.Value);
        }

        private void SpawnHealthBar(LocalTransform transform, SelectionFeedbackOffset selectionFeedbackOffset,
            MaxHitPointsComponent maxHitPoints, EntityCommandBuffer ecb, Entity entity, ElementTeamComponent team, PhysicsCollider collider, PhysicsWorldSingleton physicsWorld)
        {
            UnitUIController uiPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().UnitUI;
            float3 spawnPosition = transform.Position;
            UnitUIController elementUI = Object.Instantiate(uiPrefab, spawnPosition, Quaternion.identity);
            elementUI.UpdateHealthBar(maxHitPoints.Value, maxHitPoints.Value);
            SetUIColor(team, elementUI);
            RigidTransform rigidTransform = new RigidTransform(transform.Rotation, transform.Position);
            Aabb aabb = collider.Value.Value.CalculateAabb(rigidTransform);
            float2 colliderSize = new float2(aabb.Extents.x * 2f, aabb.Extents.z * 2f);
            elementUI.SetRectTransform(colliderSize.x, colliderSize.y);
            elementUI.SetHealthBarOffset(selectionFeedbackOffset.HealthBarOffset);
            ecb.AddComponent(entity, new HealthBarUIReferenceComponent() { Value = elementUI });
        }

        private void SetUIColor(ElementTeamComponent team, UnitUIController elementUI)
        {
            UITeamColors uiColors = SystemAPI.ManagedAPI.GetSingleton<UITeamColors>();
            
            if (team.Team is TeamType.Red)
            {
                elementUI.SetTeamColor(uiColors.RedColor);
            }
            else
            {
                elementUI.SetTeamColor(uiColors.BlueColor);
            }
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