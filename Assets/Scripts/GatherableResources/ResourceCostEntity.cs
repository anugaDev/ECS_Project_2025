using System;
using Types;
using UnityEngine;

namespace GatherableResources
{
    [Serializable]
    public class ResourceCostEntity
    {
        [SerializeField]
        private ResourceType _resourceType;
        
        [SerializeField]
        private int _cost;

        public ResourceType ResourceType => _resourceType;

        public int Cost => _cost;
    }
}