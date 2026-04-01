using Unity.Entities;

namespace Navigation
{
    public class NavMeshObstacleReference : IComponentData
    {
        public UnityEngine.GameObject ObstacleGameObject;
    }

    public class NavMeshObstacleCleanupRef : ICleanupComponentData
    {
        public UnityEngine.GameObject ObstacleGameObject;
    }

    public struct NavMeshProcessedTag : IComponentData
    {
    }
}

