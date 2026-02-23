using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    /// <summary>
    /// DISABLED: PathComponent and PathWaypointBuffer now stay on server entities.
    ///
    /// ServerUnitMoveSystem uses PathComponent.CurrentWaypointIndex to track
    /// server-side progress through the waypoints received via UnitWaypointsInputComponent.
    /// Removing PathComponent would break that progress tracking.
    /// </summary>
    // System intentionally not registered — kept as reference/documentation only.
    // [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    // [UpdateInGroup(typeof(InitializationSystemGroup))]
    // public partial struct RemoveClientOnlyComponentsOnServerSystem : ISystem { ... }
}
