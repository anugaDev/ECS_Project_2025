using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    /// <summary>
    /// Ensures path components exist on units on the client.
    /// 
    /// WHY: PathComponent and PathWaypointBuffer are needed for client-side
    /// NavMesh pathfinding. This system adds them if they're missing after
    /// a unit is spawned on the client.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnsurePathComponentsOnClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            // Find all units that don't have path components yet
            foreach (var (unitTag, entity) in SystemAPI.Query<RefRO<UnitTagComponent>>()
                .WithNone<PathComponent>()
                .WithEntityAccess())
            {
                // Add path components - client needs them for NavMesh pathfinding
                ecb.AddComponent<PathComponent>(entity);
                ecb.AddBuffer<PathWaypointBuffer>(entity);

                UnityEngine.Debug.Log($"[Client] Added path components to unit {entity.Index}");
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

