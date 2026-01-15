using ElementCommons;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Player
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeLocalPlayerSystem : ISystem 
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((PlayerTagComponent _, Entity entity) 
                     in SystemAPI.Query<PlayerTagComponent>().WithAll<GhostOwnerIsLocal>().WithNone<OwnerTagComponent>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<OwnerTagComponent>(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}