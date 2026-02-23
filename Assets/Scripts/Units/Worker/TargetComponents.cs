using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Units.Worker
{
    /// <summary>
    /// Component set by player input systems to request a target change
    /// </summary>
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
    }

    /// <summary>
    /// Component set by server-side AI systems (worker, attack) to request a target change
    /// </summary>
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
        public bool HasNewTarget;
    }

    /// <summary>
    /// The current active target for the unit - single source of truth
    /// Set by TargetSystem which merges input and server targets
    /// NOT synchronized - both client and server calculate independently from synchronized inputs
    /// This prevents server from overwriting client's predicted values
    /// </summary>
    public struct CurrentTargetComponent : IComponentData
    {
        // NO GhostFields - these are NOT synchronized over the network
        // Both client and server calculate them independently from SetInputStateTargetComponent
        public Entity TargetEntity;
        public float3 TargetPosition;
        public bool IsFollowingTarget;
        public float StoppingDistance;

        // Calculated independently on client and server by UnitMoveSystem
        // Client uses NavMesh paths, server might use different logic
        public bool IsTargetReached;
    }
}

