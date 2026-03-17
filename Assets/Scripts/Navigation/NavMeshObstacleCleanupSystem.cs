using Navigation;
using Units;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Navigation
{
    /// <summary>
    /// Detects buildings and trees that have been destroyed (including via NetCode ghost despawn)
    /// by querying for NavMeshObstacleCleanupRef WITHOUT LocalTransform.
    ///
    /// When an entity is destroyed, all regular components (LocalTransform, etc.) are stripped,
    /// but ICleanupComponentData components persist — turning it into a "cleanup entity".
    /// This system finds those cleanup entities, destroys their companion obstacle GameObjects,
    /// removes the cleanup component (allowing ECS to fully discard the entity), and rebuilds
    /// the NavMesh so the carved hole disappears.
    /// </summary>
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

            // Collect entities whose regular components are gone (destroyed) but cleanup ref persists.
            // We cannot call EntityManager.RemoveComponent inside the foreach, so gather first.
            using NativeList<Entity> toCleanup = new NativeList<Entity>(Allocator.Temp);

            foreach ((NavMeshObstacleCleanupRef cleanupRef, Entity entity) in SystemAPI
                         .Query<NavMeshObstacleCleanupRef>()
                         .WithNone<LocalTransform>()   // LocalTransform is gone → entity was destroyed
                         .WithEntityAccess())
            {
                DestroyObstacleGameObject(cleanupRef);
                toCleanup.Add(entity);
                rebuildNeeded = true;
            }

            // Remove the cleanup component so ECS can fully discard the entity.
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
