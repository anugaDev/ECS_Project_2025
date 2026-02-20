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
        private const float REBUILD_DELAY = 0.5f;

        private NavMeshSurface _navMeshSurface;

        private GameObject _groundPlane;

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
                if (GroundPlaneSetup.Instance != null)
                {
                    _groundPlane = GroundPlaneSetup.Instance.gameObject;
                }

                // CRITICAL: Check if NavMeshSurface has baked data
                if (_navMeshSurface.navMeshData == null)
                {
                    UnityEngine.Debug.LogError("[DynamicNavMesh] ❌ NavMeshSurface has NO navMeshData!");
                    UnityEngine.Debug.LogError("  The NavMesh was not baked or the data asset is missing!");
                }
                else
                {
                    UnityEngine.Debug.Log($"[DynamicNavMesh] NavMeshSurface has data asset: {_navMeshSurface.navMeshData.name}");

                    // CRITICAL: NavMeshSurface should automatically add its data, but let's verify
                    // Check if the data is actually in the scene
                    UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();

                    if (triangulation.vertices.Length > 100)
                    {
                        UnityEngine.Debug.Log($"[DynamicNavMesh] ✓ Using pre-baked NavMesh from SubScene!");
                        UnityEngine.Debug.Log($"  Vertices={triangulation.vertices.Length}, Triangles={triangulation.indices.Length / 3}");
                        UnityEngine.Debug.Log($"  NavMesh will NOT be rebuilt (SubScene objects are baked in editor)");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError("[DynamicNavMesh] ❌ NavMeshData exists but is NOT active in scene!");
                        UnityEngine.Debug.LogError("  This might be a NavMeshSurface bug. Trying to manually add data...");

                        // Try to manually add the NavMesh data
                        UnityEngine.AI.NavMesh.AddNavMeshData(_navMeshSurface.navMeshData);

                        // Check again
                        triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
                        if (triangulation.vertices.Length > 100)
                        {
                            UnityEngine.Debug.Log($"[DynamicNavMesh] ✓ Manually added NavMeshData! Vertices={triangulation.vertices.Length}");
                        }
                        else
                        {
                            UnityEngine.Debug.LogError("[DynamicNavMesh] ❌ Still no NavMesh! Check if NavMeshSurface is enabled.");
                        }
                    }
                }

                _initialNavMeshBuilt = true;
            }

            // Create NavMeshObstacles for buildings (both pre-placed and runtime-spawned)
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach (var (buildingComponents, transform, entity) in SystemAPI.Query<RefRO<BuildingComponents>, RefRO<LocalTransform>>()
                         .WithAll<NewBuildingTagComponent>().WithNone<NavMeshProcessedTag>().WithEntityAccess())
            {
                // Skip pre-placed buildings on first frame (they're baked into NavMesh)
                if (!_firstFrameProcessed)
                {
                    ecb.AddComponent<NavMeshProcessedTag>(entity);
                    continue;
                }

                // For runtime-spawned buildings, create NavMeshObstacle
                CreateBuildingNavMeshObstacle(entity, transform.ValueRO, buildingComponents.ValueRO);
                ecb.AddComponent<NavMeshProcessedTag>(entity);
            }

            _firstFrameProcessed = true;

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void CreateBuildingNavMeshObstacle(Entity buildingEntity, LocalTransform transform, BuildingComponents buildingComponents)
        {
            // Create a companion GameObject with NavMeshObstacle
            GameObject obstacleObj = new GameObject($"BuildingObstacle_{buildingEntity.Index}");
            obstacleObj.transform.position = transform.Position;
            obstacleObj.transform.rotation = transform.Rotation;

            // Add NavMeshObstacle component
            NavMeshObstacle obstacle = obstacleObj.AddComponent<NavMeshObstacle>();
            obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.size = new UnityEngine.Vector3(5f, 2f, 5f); // Default size - adjust based on building type
            obstacle.center = UnityEngine.Vector3.zero;
            obstacle.carving = true; // CRITICAL: Enable carving to cut holes in NavMesh
            obstacle.carveOnlyStationary = false; // Carve even if moving (for placement)

            UnityEngine.Debug.Log($"[DynamicNavMesh] Created NavMeshObstacle for building at {transform.Position}");

            // TODO: Store reference to GameObject for cleanup when building is destroyed
        }



        private void RebuildNavMesh()
        {
            if (_navMeshSurface == null)
            {
                return;
            }

            UnityEngine.Debug.Log($"[DynamicNavMesh] Rebuilding NavMesh...");
            UnityEngine.Debug.Log($"  CollectObjects: {_navMeshSurface.collectObjects}");
            UnityEngine.Debug.Log($"  UseGeometry: {_navMeshSurface.useGeometry}");
            UnityEngine.Debug.Log($"  LayerMask: {_navMeshSurface.layerMask.value}");
            UnityEngine.Debug.Log($"  Size: {_navMeshSurface.size}");
            UnityEngine.Debug.Log($"  Center: {_navMeshSurface.center}");

            // DEBUG: Check if GroundPlane exists
            GameObject groundPlane = GameObject.Find("GroundPlane");
            if (groundPlane == null)
            {
                UnityEngine.Debug.LogError("  ❌ GroundPlane GameObject NOT FOUND!");
            }
            else
            {
                UnityEngine.Debug.Log($"  ✓ GroundPlane found at position: {groundPlane.transform.position}");
                BoxCollider groundCollider = groundPlane.GetComponent<BoxCollider>();
                if (groundCollider == null)
                {
                    UnityEngine.Debug.LogError("  ❌ GroundPlane has NO BoxCollider!");
                }
                else
                {
                    UnityEngine.Debug.Log($"  ✓ GroundPlane has BoxCollider: size={groundCollider.size}, center={groundCollider.center}");
                    UnityEngine.Debug.Log($"  ✓ GroundPlane layer: {groundPlane.layer}");
                    UnityEngine.Debug.Log($"  ✓ GroundPlane enabled: {groundCollider.enabled}");
                    UnityEngine.Debug.Log($"  ✓ GroundPlane isTrigger: {groundCollider.isTrigger}");
                    UnityEngine.Debug.Log($"  ✓ GroundPlane bounds: {groundCollider.bounds}");

                    // Check if layer is in mask
                    int layerBit = 1 << groundPlane.layer;
                    bool layerIncluded = (_navMeshSurface.layerMask.value & layerBit) != 0;
                    UnityEngine.Debug.Log($"  ✓ Layer {groundPlane.layer} included in LayerMask: {layerIncluded}");
                }
            }

            // DEBUG: Check what colliders are in the volume
            Collider[] colliders = Physics.OverlapBox(
                _navMeshSurface.center,
                _navMeshSurface.size / 2f,
                Quaternion.identity,
                _navMeshSurface.layerMask
            );
            UnityEngine.Debug.Log($"  Colliders in volume: {colliders.Length}");
            foreach (Collider col in colliders)
            {
                UnityEngine.Debug.Log($"    - {col.gameObject.name} (Layer: {col.gameObject.layer})");
            }

            _navMeshSurface.BuildNavMesh();

            UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
            UnityEngine.Debug.Log($"[DynamicNavMesh] NavMesh rebuilt! Vertices: {triangulation.vertices.Length}, Triangles: {triangulation.indices.Length / 3}");

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

