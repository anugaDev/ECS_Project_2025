using Unity.Entities;
using Unity.Mathematics;

namespace Navigation
{
    public struct NavMeshObstacleComponent : IComponentData
    {
        public float3 Size;

        public bool Carve;
    }

    public class NavMeshObstacleReference : IComponentData
    {
        public UnityEngine.GameObject ObstacleGameObject;
    }

    /// <summary>
    /// Managed cleanup component — survives entity destruction (including NetCode ghost despawn).
    /// Used to track the companion NavMeshObstacle GameObject so it can be destroyed and the
    /// NavMesh rebuilt when the owning building or tree entity is despawned.
    /// </summary>
    public class NavMeshObstacleCleanupRef : ICleanupComponentData
    {
        public UnityEngine.GameObject ObstacleGameObject;
    }

    public struct NavMeshProcessedTag : IComponentData
    {
    }

    internal struct BuildingObstacleData
    {
        public quaternion Rotation;

        public float3 Position;

        public Entity Entity;

        public float3 Size;
    }
}

