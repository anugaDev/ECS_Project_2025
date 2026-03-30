using GatherableResources;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Navigation
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DynamicNavMeshSystem))]
    public partial class ResourceNavMeshObstacleSystem : SystemBase
    {
        private NavMeshObstacle _templateObstacle;

        protected override void OnCreate()
        {
            base.OnCreate();

            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (obj.GetComponent<ResourceAuthoring>() != null)
                {
                    NavMeshObstacle obstacle = obj.GetComponent<NavMeshObstacle>();
                    if (obstacle != null)
                    {
                        _templateObstacle = obstacle;
                        UnityEngine.Debug.Log($"[ResourceNavMeshObstacle] Found template obstacle: shape={obstacle.shape}, radius={obstacle.radius}, height={obstacle.height}, carving={obstacle.carving}");
                        break;
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            using NativeList<Entity> pendingEntities = new NativeList<Entity>(Unity.Collections.Allocator.Temp);
            System.Collections.Generic.List<GameObject> pendingObstacles = new System.Collections.Generic.List<GameObject>();

            foreach (var (resourceType, transform, entity) in SystemAPI
                .Query<RefRO<ResourceTypeComponent>, RefRO<LocalTransform>>()
                .WithNone<NavMeshProcessedTag>()
                .WithEntityAccess())
            {
                if (resourceType.ValueRO.Type == Types.ResourceType.Wood)
                {
                    GameObject obstacleObj = CreateTreeNavMeshObstacle(transform.ValueRO);
                    pendingEntities.Add(entity);
                    pendingObstacles.Add(obstacleObj);
                    ecb.AddComponent<NavMeshProcessedTag>(entity);
                }
            }

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

        private GameObject CreateTreeNavMeshObstacle(LocalTransform transform)
        {
            GameObject obstacleObj = new GameObject($"TreeObstacle_{transform.Position.x:F0}_{transform.Position.z:F0}");
            obstacleObj.transform.position = transform.Position;
            obstacleObj.transform.rotation = transform.Rotation;

            NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();

            if (_templateObstacle != null)
            {
                obstacle.shape = _templateObstacle.shape;
                obstacle.center = _templateObstacle.center;
                obstacle.size = _templateObstacle.size;
                obstacle.radius = _templateObstacle.radius;
                obstacle.height = _templateObstacle.height;
                obstacle.carving = _templateObstacle.carving;
                obstacle.carveOnlyStationary = _templateObstacle.carveOnlyStationary;
                obstacle.carvingMoveThreshold = _templateObstacle.carvingMoveThreshold;
                obstacle.carvingTimeToStationary = _templateObstacle.carvingTimeToStationary;

                UnityEngine.Debug.Log($"[ResourceNavMeshObstacle] Created obstacle for tree at {transform.Position} (copied from template)");
            }
            else
            {
                obstacle.shape = NavMeshObstacleShape.Capsule;
                obstacle.radius = 0.5f;
                obstacle.height = 1f;
                obstacle.center = new Vector3(0, 0.83f, 0);
                obstacle.carving = true;
                obstacle.carveOnlyStationary = true;

                UnityEngine.Debug.LogWarning($"[ResourceNavMeshObstacle] No template found, using fallback values");
            }

            return obstacleObj;
        }
    }
}

