using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial struct EnsurePathComponentsOnClientSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach (var (unitTag, entity) in SystemAPI.Query<RefRO<UnitTagComponent>>()
                .WithNone<PathComponent>()
                .WithEntityAccess())
            {
                ecb.AddComponent<PathComponent>(entity);
                ecb.AddBuffer<PathWaypointBuffer>(entity);
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

