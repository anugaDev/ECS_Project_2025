using System.Collections.Generic;
using ElementCommons;
using PlayerInputs;
using ScriptableObjects;
using Types;
using Units.Worker;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;

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
            foreach ((LocalTransform transform, UnitTagComponent _, UnitTypeComponent unitTypeComponent, Entity entity)
                     in SystemAPI.Query<LocalTransform, UnitTagComponent, UnitTypeComponent>().WithAll<GhostOwnerIsLocal>().WithNone<OwnerTagComponent>().WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<OwnerTagComponent>(entity);
                entityCommandBuffer.SetComponent(entity, GetTargetPositionComponent(transform));
                entityCommandBuffer.SetComponent(entity, GetDetailsComponent(unitTypeComponent));
            }

            entityCommandBuffer.Playback(state.EntityManager);
        }

        private ElementDisplayDetailsComponent GetDetailsComponent(UnitTypeComponent unitTypeComponent)
        {
            UnitType unitType = unitTypeComponent.Type;
            UnitsConfigurationComponent configurationComponent = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>();
            Dictionary<UnitType, UnitScriptableObject> unitScriptableObjects = configurationComponent.Configuration.GetUnitsDictionary();
            string displayName = unitScriptableObjects[unitType].Name;
            Sprite displayImage = unitScriptableObjects[unitType].Sprite;
            return new ElementDisplayDetailsComponent
            {
                Name = displayName,
                Sprite = displayImage
            };
        }

        private SetInputStateTargetComponent GetTargetPositionComponent(LocalTransform transform)
        {
            return new SetInputStateTargetComponent
            {
                TargetPosition = transform.Position
            };
        }
    }
}