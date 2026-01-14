using Types;
using UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;
using UnityEngine;
using Utils;

namespace Server
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializePlayerSystem : ISystem
    {
        private EntityCommandBuffer _entityCommandBuffer;

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((PlayerTagComponent playerTag, EntityTeamComponent playerTeam, Entity unitEntity) 
                     in SystemAPI.Query<PlayerTagComponent, 
                         EntityTeamComponent>().WithAny<NewUnitTagComponent>().WithEntityAccess())
            {

            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }
    }
}