using Types;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace GatherableResources
{
    public struct ResourceTypeComponent : IComponentData
    {
        [GhostField]
        public ResourceType Type;
    }

    public struct MaxResourceQuantityComponent : IComponentData
    {
        [GhostField]
        public int Value;
    }

    public struct CurrentResourceQuantityComponent : IComponentData
    {
        [GhostField]
        public int Value;
    }
    
    public class ResourceUIReferenceComponent : ICleanupComponentData
    {
        public GameObject Instance;
    }

}