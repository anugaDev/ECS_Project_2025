using Types;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Rendering;

namespace Server
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    public partial struct InitializeUnitSystem : ISystem
    {
        private EntityCommandBuffer _entityCommandBuffer;

        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((RefRW<PhysicsMass> physicsMass, UnitComponents.UnitTeam unitTeam, Entity unitEntity) 
                     in SystemAPI.Query<RefRW<PhysicsMass>, 
                         UnitComponents.UnitTeam>().WithAny<UnitComponents.NewUnitTagComponent>().WithEntityAccess())
            {
                SetPhysicsValues(physicsMass);
                SetTeamColor(unitEntity, unitTeam);

            }
            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void SetTeamColor(Entity unitEntity, UnitComponents.UnitTeam unitTeam)
        {
            float4 teamColor = new float4(0,0,1,1);

            if (unitTeam.Team is TeamType.Blue)
            {
                teamColor = new float4(0,0,1,1);
            }

            URPMaterialPropertyBaseColor unitColor = new URPMaterialPropertyBaseColor();
            unitColor.Value = teamColor;
            _entityCommandBuffer.SetComponent(unitEntity, unitColor);
        }

        private static void SetPhysicsValues(RefRW<PhysicsMass> physicsMass)
        {
            physicsMass.ValueRW.InverseInertia[0] = 0;
            physicsMass.ValueRW.InverseInertia[1] = 0;
            physicsMass.ValueRW.InverseInertia[2] = 0;
        }
    }
}