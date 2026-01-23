using UI;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace GatherableResources
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct ResourceFeedbackSystem : ISystem
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

            foreach ((LocalTransform transform, ResourceTypeComponent _, Entity entity) in SystemAPI
                         .Query<LocalTransform, ResourceTypeComponent>().WithNone<ResourceUIReferenceComponent>().WithEntityAccess())
            {
                SpawnResourceFeedback(transform, ecb, entity);
            }

            foreach ((ResourceUIReferenceComponent healthBarUI, Entity entity) in SystemAPI
                         .Query<ResourceUIReferenceComponent>().WithNone<LocalTransform>()
                         .WithEntityAccess())
            {
                CleanupResourceFeedback(healthBarUI, ecb, entity);
            }
        }

        private void SpawnResourceFeedback(LocalTransform transform, EntityCommandBuffer ecb, Entity entity)
        {
            GameObject uiPrefab = SystemAPI.ManagedAPI.GetSingleton<UIPrefabs>().ResourceUI;
            float3 spawnPosition = transform.Position;
            GameObject elementUI = Object.Instantiate(uiPrefab, spawnPosition, Quaternion.identity);
            ecb.AddComponent(entity, new ResourceUIReferenceComponent() { Instance = elementUI });
        }

        private void CleanupResourceFeedback(ResourceUIReferenceComponent resourceUI, EntityCommandBuffer ecb, Entity entity)
        {
            Object.Destroy(resourceUI.Instance);
            ecb.RemoveComponent<ResourceUIReferenceComponent>(entity);
        }
    }
}