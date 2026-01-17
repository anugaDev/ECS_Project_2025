using ElementCommons;
using UI;
using Units;
using Unity.Collections;
using Unity.Entities;

namespace Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializePlayerSystem : ISystem
    {
        private EntityCommandBuffer _entityCommandBuffer;

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((PlayerTagComponent playerTag, Entity unitEntity) 
                     in SystemAPI.Query<PlayerTagComponent>().WithNone<OwnerTagComponent>().WithEntityAccess())
            { 
                _entityCommandBuffer.AddComponent<OwnerTagComponent>(unitEntity);
            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}