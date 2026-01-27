using Types;
using Unity.Entities;
using Unity.NetCode;

namespace Units.Worker
{
    public struct WorkerStoringTagComponent : IComponentData
    {
        [GhostField]
        public Entity BuildingEntity;

        [GhostField]
        public Entity PreviousResourceEntity;
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

    public struct CurrentResourceQuantityComponent : IComponentData
    {
        public ResourceType ResourceType;

        public int Value;
    }
    
    public struct PreviousResourceEntityComponent : IComponentData
    {
        public Entity ResourceEntity;
    }
}