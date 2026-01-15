using Buildings;
using ElementCommons;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeLocalBuildingSystem : ISystem 
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((BuildingTagComponent _, Entity entity) 
                     in SystemAPI.Query<BuildingTagComponent>().WithAll<GhostOwnerIsLocal>().WithNone<OwnerTagComponent>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<OwnerTagComponent>(entity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}