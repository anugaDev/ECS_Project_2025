using ElementCommons;
using PlayerInputs;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Units
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct InitializeLocalUnitSystem : ISystem 
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkId>();
        }
        
        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((LocalTransform transform, UnitTagComponent _, Entity entity) 
                     in SystemAPI.Query<LocalTransform, UnitTagComponent>().WithAll<GhostOwnerIsLocal>().WithNone<OwnerTagComponent>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<OwnerTagComponent>(entity);
                entityCommandBuffer.SetComponent(entity, GetTargetPositionComponent(transform));
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }

        private UnitTargetPositionComponent GetTargetPositionComponent(LocalTransform transform)
        {
            return new UnitTargetPositionComponent
            {
                Value = transform.Position
            };
        }
    }
}