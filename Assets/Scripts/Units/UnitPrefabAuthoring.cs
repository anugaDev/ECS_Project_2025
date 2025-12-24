using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class UnitPrefabAuthoring : MonoBehaviour
    { 
        [SerializeField] 
        GameObject Unit;
        
        public class UnitPrefabBaker : Baker<UnitPrefabAuthoring>
        {
            public override void Bake(UnitPrefabAuthoring prefabAuthoring)
            { 
                Entity unitContainer = GetEntity(TransformUsageFlags.None);
                UnitPrefabComponent unitComponent = GetUnitComponent(prefabAuthoring);
                AddComponent(unitContainer, unitComponent);
            }

            private UnitPrefabComponent GetUnitComponent(UnitPrefabAuthoring prefabAuthoring)
            {
                return new UnitPrefabComponent
                {
                    Unit = GetEntity(prefabAuthoring.Unit, TransformUsageFlags.Dynamic)
                };
            }
        }
    }
}