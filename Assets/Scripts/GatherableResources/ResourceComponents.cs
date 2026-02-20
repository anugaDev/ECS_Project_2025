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

    /// <summary>
    /// Keeps a reference to the tree GameObject so its CapsuleCollider stays alive for NavMesh.
    /// Without this, the GameObject is destroyed during baking and NavMesh can't see the tree.
    /// </summary>
    public class ResourceGameObjectReference : IComponentData
    {
        public GameObject GameObject;
    }
}