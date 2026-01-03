using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace UI
{
    public class HealthBarUIReferenceComponent : ICleanupComponentData
    {
        public UnitUIController Value;
    }

    public struct HealthBarOffsetComponent : IComponentData
    {
        public float3 Value;
    }
    public struct HealthBarSpawnedTagComponent : IComponentData
    {
    }
}