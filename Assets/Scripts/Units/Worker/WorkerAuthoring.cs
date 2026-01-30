using Types;
using Unity.Entities;
using Units.Worker;
using UnityEngine;

namespace Units
{
    public class WorkerAuthoring : MonoBehaviour
    {
        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(unitEntity, new CurrentWorkerResourceQuantityComponent
                {
                    ResourceType = ResourceType.None,
                    Value = 0,
                    PreviousResourceEntity = Entity.Null
                });
            }
        }
    }
}