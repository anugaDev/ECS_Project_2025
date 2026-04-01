using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Units.Worker
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct SetInputStateTargetComponent : IInputComponentData
    {
        [GhostField]
        public Entity TargetEntity;

        [GhostField]
        public float3 TargetPosition;

        [GhostField]
        public bool IsFollowingTarget;

        [GhostField]
        public float StoppingDistance;

        [GhostField]
        public bool HasNewTarget;
        
        [GhostField]
        public int TargetVersion;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct SetServerStateTargetComponent : IComponentData
    {
        [GhostField]
        public Entity TargetEntity;

        [GhostField]
        public float3 TargetPosition;

        [GhostField]
        public bool IsFollowingTarget;

        [GhostField]
        public float StoppingDistance;

        [GhostField]
        public int TargetVersion;
    }
}

