using UI;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class UnitPrefabAuthoring : MonoBehaviour
    { 
        [SerializeField] 
        GameObject Unit;
        
        [SerializeField] 
        public HealthBarView HealthBarPrefab;
        
        public class UnitPrefabBaker : Baker<UnitPrefabAuthoring>
        {
            public override void Bake(UnitPrefabAuthoring prefabAuthoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                Entity unitContainer = GetEntity(TransformUsageFlags.None);
                AddComponent(unitContainer, GetUnitComponent(prefabAuthoring));
                AddComponentObject(prefabContainerEntity, GetUIPrefabs(prefabAuthoring));
            }

            private UIPrefabs GetUIPrefabs(UnitPrefabAuthoring prefabAuthoring)
            {
                return new UIPrefabs
                {
                    HealthBar =  prefabAuthoring.HealthBarPrefab
                };
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