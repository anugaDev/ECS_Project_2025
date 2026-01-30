using Types;
using Unity.Entities;
using Unity.NetCode;

namespace Units.Worker
{
    public struct WorkerStoringTagComponent : IComponentData
    {
        [GhostField]
        public Entity BuildingEntity;
    }

    public struct WorkerConstructionTagComponent : IComponentData
    {
        [GhostField]
        public Entity BuildingEntity;
    }

    public struct WorkerGatheringTagComponent : IComponentData
    {
        [GhostField]
        public Entity ResourceEntity;
    }

    public struct CurrentWorkerResourceQuantityComponent : IComponentData
    {
        [GhostField]
        public ResourceType ResourceType;

        [GhostField]
        public int Value;

        [GhostField]
        public Entity PreviousResourceEntity;
    }

    public struct WorkerActionCooldownComponent : IComponentData
    {
        [GhostField]
        public double CooldownEndTime;
    }
}