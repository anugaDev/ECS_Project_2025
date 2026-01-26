using System.Collections.Generic;
using ScriptableObjects;
using Types;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class UnitPrefabAuthoring : MonoBehaviour
    { 
        [SerializeField] 
        private UnitsScriptableObject _unitsConfiguration;

        public UnitsScriptableObject UnitsConfiguration => _unitsConfiguration;

        public class UnitPrefabBaker : Baker<UnitPrefabAuthoring>
        {
            public override void Bake(UnitPrefabAuthoring prefabAuthoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                Entity unitContainer = GetEntity(TransformUsageFlags.None);
                AddComponent(unitContainer, GetUnitComponent(prefabAuthoring));
                AddComponentObject(prefabContainerEntity, GetUnitsConfiguration(prefabAuthoring));
                AddComponentObject(prefabContainerEntity, GetUITeamColors());
            }

            private UITeamColors GetUITeamColors()
            {
                return new UITeamColors
                {
                    RedColor = Color.red,
                    BlueColor = Color.blue
                };
            }

            private UnitsConfigurationComponent GetUnitsConfiguration(UnitPrefabAuthoring prefabAuthoring)
            {
                return new UnitsConfigurationComponent
                {
                    Configuration = prefabAuthoring.UnitsConfiguration
                };
            }

            private UnitPrefabComponent GetUnitComponent(UnitPrefabAuthoring prefabAuthoring)
            {
                Dictionary<UnitType, UnitScriptableObject> unitsDictionary = prefabAuthoring.UnitsConfiguration.GetUnitsDictionary();

                return new UnitPrefabComponent
                {
                    Ballista = GetEntity(unitsDictionary[UnitType.Ballista].UnitPrefab, TransformUsageFlags.Dynamic),
                    Worker = GetEntity(unitsDictionary[UnitType.Worker].UnitPrefab, TransformUsageFlags.Dynamic),
                    Warrior = GetEntity(unitsDictionary[UnitType.Warrior].UnitPrefab, TransformUsageFlags.Dynamic),
                    Archer = GetEntity(unitsDictionary[UnitType.Archer].UnitPrefab, TransformUsageFlags.Dynamic)
                };
            }
        }
    }
}