using Navigation;
using Units;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Navigation
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DynamicNavMeshSystem))]
    public partial class NavMeshObstacleCleanupSystem : SystemBase
    {
        private NavMeshSurface _navMeshSurface;

        protected override void OnStartRunning()
        {
            _navMeshSurface = GameObject.FindObjectOfType<NavMeshSurface>();
        }

        protected override void OnUpdate()
        {
            bool rebuildNeeded = false;

            using NativeList<Entity> toCleanup = new NativeList<Entity>(Allocator.Temp);

            foreach ((NavMeshObstacleCleanupRef cleanupRef, Entity entity) in SystemAPI
                         .Query<NavMeshObstacleCleanupRef>()
                         .WithNone<LocalTransform>()                         .WithEntityAccess())
            {
                DestroyObstacleGameObject(cleanupRef);
                toCleanup.Add(entity);
                rebuildNeeded = true;
            }

            for (int i = 0; i < toCleanup.Length; i++)
            {
                EntityManager.RemoveComponent<NavMeshObstacleCleanupRef>(toCleanup[i]);
            }

            if (rebuildNeeded)
            {
                RebuildNavMesh();
            }
        }

        private void DestroyObstacleGameObject(NavMeshObstacleCleanupRef cleanupRef)
        {
            if (cleanupRef.ObstacleGameObject == null)
            {
                return;
            }

            GameObject.Destroy(cleanupRef.ObstacleGameObject);
        }

        private void RebuildNavMesh()
        {
            if (_navMeshSurface == null)
            {
                return;
            }

            _navMeshSurface.BuildNavMesh();
            InvalidateAllPaths();
        }

        private void InvalidateAllPaths()
        {
            foreach ((RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer)
                     in SystemAPI.Query<RefRW<PathComponent>, DynamicBuffer<PathWaypointBuffer>>())
            {
                if (!pathComponent.ValueRO.HasPath)
                {
                    continue;
                }

                pathComponent.ValueRW.HasPath = false;
                pathComponent.ValueRW.CurrentWaypointIndex = 0;
                pathBuffer.Clear();
            }
        }
    }
}
