using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class HealthBarUIReferenceComponent : ICleanupComponentData
    {
        public HealthBarView Value;
    }

    public struct HealthBarOffsetComponent : IComponentData
    {
        public float3 Value;
    }
}