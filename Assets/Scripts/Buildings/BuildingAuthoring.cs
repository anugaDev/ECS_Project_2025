using PlayerInputs;
using Types;
using Units;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Buildings
{
    public class BuildingAuthoring : MonoBehaviour
    {
        [SerializeField]
        private BuildingType _buildingType;

        public BuildingType BuildingType => _buildingType;
        
        public class UnitBaker : Baker<BuildingAuthoring>
        {
            public override void Bake(BuildingAuthoring authoring)
            {
                Entity buildingEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BuildingTagComponent>(buildingEntity);
                AddComponent<NewBuildingTagComponent>(buildingEntity);
                AddComponent<EntityTeamComponent>(buildingEntity);
                AddComponent<URPMaterialPropertyBaseColor>(buildingEntity);
                AddComponent<EntitySelectionComponent>(buildingEntity);
                AddComponent(buildingEntity, GetUnitTypeComponent(authoring));
            }

            private BuildingTypeComponent GetUnitTypeComponent(BuildingAuthoring authoring)
            {
                return new BuildingTypeComponent
                {
                    Type = authoring.BuildingType
                };
            }
        }

    }
}