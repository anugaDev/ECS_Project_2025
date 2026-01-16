using System.Collections.Generic;
using ElementCommons;
using PlayerInputs;
using ScriptableObjects;
using Types;
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
            foreach ((LocalTransform transform, UnitTagComponent _, UnitTypeComponent UnitTypeComponent, Entity entity) 
                     in SystemAPI.Query<LocalTransform, UnitTagComponent, UnitTypeComponent>().WithAll<GhostOwnerIsLocal>().WithNone<OwnerTagComponent>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<OwnerTagComponent>(entity);
                entityCommandBuffer.SetComponent(entity, GetTargetPositionComponent(transform));
                entityCommandBuffer.SetComponent(entity, GetDetailsComponent(UnitTypeComponent));
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }

        private ElementDisplayDetailsComponent GetDetailsComponent(UnitTypeComponent unitTypeComponent)
        {
            UnitType unitType = unitTypeComponent.Type;
            UnitsConfigurationComponent configurationComponent = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>();
            Dictionary<UnitType, UnitScriptableObject> unitScriptableObjects = configurationComponent.Configuration.GetUnitsDictionary();
            string displayName = unitScriptableObjects[unitType].Name;
            return new ElementDisplayDetailsComponent
            {
                Name = displayName
            };
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