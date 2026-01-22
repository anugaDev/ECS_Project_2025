using Buildings;
using Units;
using Unity.AI.Navigation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Navigation
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(Buildings.InitializeBuildingSystem))]
    public partial class DynamicNavMeshSystem : SystemBase
    {
        private const float REBUILD_DELAY = 0.5f;

        private NavMeshSurface _navMeshSurface;

        private GameObject _groundPlane;

        private double _lastChangeTime;

        private bool _rebuildPending;

        private bool _initialNavMeshBuilt;

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
                if (GroundPlaneSetup.Instance != null)
                {
                    _groundPlane = GroundPlaneSetup.Instance.gameObject;
                }

                RebuildNavMesh();
                _initialNavMeshBuilt = true;
            }

            bool foundNewBuilding = false;
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var buildingReference in SystemAPI.Query<RefRO<BuildingComponents>>()
                         .WithAll<NewBuildingTagComponent>().WithNone<NavMeshProcessedTag>() .WithEntityAccess())
            {
                foundNewBuilding = true;
                Entity entity = buildingReference.Item2;
                ecb.AddComponent<NavMeshProcessedTag>(entity);
            }

            if (foundNewBuilding)
            {
                _rebuildPending = true;
                _lastChangeTime = SystemAPI.Time.ElapsedTime;
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();

            if (_rebuildPending && SystemAPI.Time.ElapsedTime - _lastChangeTime >= REBUILD_DELAY)
            {
                RebuildNavMesh();
                _rebuildPending = false;
            }
        }

        private void CreateNavMeshObstacle(BuildingObstacleData data)
        {
            GameObject obstacleObj = new GameObject($"NavObstacle_{data.Entity.Index}");
            obstacleObj.transform.position = data.Position;
            obstacleObj.transform.rotation = data.Rotation;
            obstacleObj.layer = 0;

            BoxCollider boxCollider = obstacleObj.AddComponent<BoxCollider>();
            boxCollider.size = data.Size;
            boxCollider.center = Vector3.zero;
            boxCollider.isTrigger = false;

            NavMeshModifier modifier = obstacleObj.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 1;

            GameObject debugCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            debugCube.transform.SetParent(obstacleObj.transform);
            debugCube.transform.localPosition = Vector3.zero;
            debugCube.transform.localScale = data.Size;
            debugCube.GetComponent<Renderer>().material.color = Color.red;
            Object.Destroy(debugCube.GetComponent<BoxCollider>());

            EntityManager.AddComponentObject(data.Entity, new NavMeshObstacleReference
            {
                ObstacleGameObject = obstacleObj
            });
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
            foreach (var (pathComponent, pathBuffer)
                     in SystemAPI.Query<RefRW<PathComponent>, DynamicBuffer<PathWaypointBuffer>>())
            {
                InvalidatePath(pathComponent, pathBuffer);
            }
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

