using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Units
{
    public class UnitAuthoring : MonoBehaviour
    {
        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UnitComponents.UnitTagComponent>(unitEntity);
                AddComponent<UnitComponents.NewUnitTagComponent>(unitEntity);
                AddComponent<UnitComponents.UnitTeam>(unitEntity);
                AddComponent<URPMaterialPropertyBaseColor>(unitEntity);
            }
        }
    }
}