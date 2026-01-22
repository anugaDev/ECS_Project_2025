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

