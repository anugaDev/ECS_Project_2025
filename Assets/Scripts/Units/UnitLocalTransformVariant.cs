using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    /// <summary>
    /// Ghost component variant for LocalTransform on units.
    /// Makes LocalTransform CLIENT-PREDICTED ONLY (not synchronized from server).
    /// 
    /// This prevents server-client oscillation when:
    /// - Client calculates NavMesh paths and moves units
    /// - Server doesn't have NavMesh and shouldn't override client movement
    /// 
    /// The client owns the movement simulation, server just receives final positions.
    /// </summary>
    [GhostComponentVariation(typeof(LocalTransform), "Unit - Client Predicted Only")]
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient, SendDataForChildEntity = false)]
    public struct UnitLocalTransformVariant
    {
        // Empty struct - this is just a marker to override LocalTransform synchronization
        // PrefabType = PredictedClient means:
        // - Only exists on predicted (owner) client
        // - NOT synchronized from server
        // - Server doesn't send this component to clients
    }
}

