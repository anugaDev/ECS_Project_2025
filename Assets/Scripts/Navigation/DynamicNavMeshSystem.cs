using Buildings;
using Units;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Navigation
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Buildings.InitializeBuildingSystem))]
    public partial class DynamicNavMeshSystem : SystemBase
    {
        private NavMeshSurface _navMeshSurface;


        private double _lastChangeTime;

        private bool _rebuildPending;

        private bool _initialNavMeshBuilt;

        private bool _firstFrameProcessed;

        protected override void OnStartRunning()
        {
            _navMeshSurface = GameObject.FindObjectOfType<NavMeshSurface>();
        }

        protected override void OnUpdate()
        {
            if (_navMeshSurface == null)
            {
                return;
            }

            if (!_initialNavMeshBuilt)
            {
                if (_navMeshSurface.navMeshData != null)
                {
                    NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

                    if (triangulation.vertices.Length <= 100)
                    {
                        NavMesh.AddNavMeshData(_navMeshSurface.navMeshData);
                    }
                }
                _initialNavMeshBuilt = true;
            }

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            using NativeList<Entity> pendingEntities = new NativeList<Entity>(Allocator.Temp);
            System.Collections.Generic.List<GameObject> pendingObstacles = new System.Collections.Generic.List<GameObject>();

            foreach (var (buildingComponents, transform, entity) in SystemAPI.Query<RefRO<BuildingComponents>, RefRO<LocalTransform>>()
                         .WithAll<NewBuildingTagComponent>().WithNone<NavMeshProcessedTag>().WithEntityAccess())
            {
                if (!_firstFrameProcessed)
                {
                    ecb.AddComponent<NavMeshProcessedTag>(entity);
                    continue;
                }

                GameObject obstacleObj = CreateBuildingNavMeshObstacle(entity, transform.ValueRO, buildingComponents.ValueRO);
                pendingEntities.Add(entity);
                pendingObstacles.Add(obstacleObj);
                ecb.AddComponent<NavMeshProcessedTag>(entity);
            }

            _firstFrameProcessed = true;

            ecb.Playback(EntityManager);
            ecb.Dispose();

            for (int i = 0; i < pendingEntities.Length; i++)
            {
                EntityManager.AddComponentObject(pendingEntities[i], new NavMeshObstacleReference
                {
                    ObstacleGameObject = pendingObstacles[i]
                });
                EntityManager.AddComponentObject(pendingEntities[i], new NavMeshObstacleCleanupRef
                {
                    ObstacleGameObject = pendingObstacles[i]
                });
            }
        }

        private GameObject CreateBuildingNavMeshObstacle(Entity buildingEntity, LocalTransform transform, BuildingComponents buildingComponents)
        {
            GameObject obstacleObj = new GameObject($"BuildingObstacle_{buildingEntity.Index}");
            obstacleObj.transform.position = transform.Position;
            obstacleObj.transform.rotation = transform.Rotation;

            NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new Vector3(5f, 2f, 5f);
            obstacle.center = Vector3.zero;
            obstacle.carving = true;
            obstacle.carveOnlyStationary = false;

            return obstacleObj;
        }

        private void InvalidatePath(RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer)
        {
            if (!pathComponent.ValueRO.HasPath)
            {
                return;
            }

            pathComponent.ValueRW.HasPath = false;
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathBuffer.Clear();
        }

        protected override void OnDestroy()
        {
            foreach (var obstacleRef in SystemAPI.Query<NavMeshObstacleReference>())
            {
                DestroyObstacle(obstacleRef);
            }
        }

        private void DestroyObstacle(NavMeshObstacleReference obstacleRef)
        {
            if (obstacleRef.ObstacleGameObject == null)
            {
                return;
            }

            GameObject.Destroy(obstacleRef.ObstacleGameObject.gameObject);
        }
    }
}

