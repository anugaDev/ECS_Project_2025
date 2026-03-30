using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [GhostComponentVariation(typeof(LocalTransform), "Unit - Client Predicted Only")]
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient, SendDataForChildEntity = false)]
    public struct UnitLocalTransformVariant
    {
    }
}

