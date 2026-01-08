using Types;
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
    public partial struct InitializeUnitSystem : ISystem
    {
        private EntityCommandBuffer _entityCommandBuffer;

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<PhysicsMass> physicsMass, EntityTeamComponent unitTeam, Entity unitEntity) 
                     in SystemAPI.Query<RefRW<PhysicsMass>, 
                         EntityTeamComponent>().WithAny<NewUnitTagComponent>().WithEntityAccess())
            {
                SetPhysicsValues(physicsMass);
                SetTeamColor(unitEntity, unitTeam);

            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void SetTeamColor(Entity unitEntity, EntityTeamComponent entityTeamComponent)
        {
            float4 teamColor = Color.red.ToFloat4();

            if (entityTeamComponent.Team is TeamType.Blue)
            {
                teamColor = Color.blue.ToFloat4();
            }

            URPMaterialPropertyBaseColor unitColor = new URPMaterialPropertyBaseColor();
            unitColor.Value = teamColor;
            _entityCommandBuffer.SetComponent(unitEntity, unitColor);
        }

        private void SetPhysicsValues(RefRW<PhysicsMass> physicsMass)
        {
            physicsMass.ValueRW.InverseInertia[0] = 0;
            physicsMass.ValueRW.InverseInertia[1] = 0;
            physicsMass.ValueRW.InverseInertia[2] = 0;
        }
    }
}