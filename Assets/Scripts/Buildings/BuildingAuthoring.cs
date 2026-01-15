using ElementCommons;
using Types;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Buildings
{
    public class BuildingAuthoring : MonoBehaviour
    {
        [SerializeField] private BuildingType _buildingType;

        [SerializeField] private SelectableElementType _selectableType;

        public BuildingType BuildingType => _buildingType;

        public SelectableElementType SelectableType => _selectableType;

        public class BuildingBaker : Baker<BuildingAuthoring>
        {
            public override void Bake(BuildingAuthoring authoring)
            {
                Entity buildingEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BuildingTagComponent>(buildingEntity);
                AddComponent<NewBuildingTagComponent>(buildingEntity);
                AddComponent<ElementTeamComponent>(buildingEntity);
                AddComponent<ElementSelectionComponent>(buildingEntity);
                AddComponent<URPMaterialPropertyBaseColor>(buildingEntity);
                AddComponent<ElementDisplayDetailsComponent>(buildingEntity);
                AddComponent(buildingEntity, GetUnitTypeComponent(authoring));
                AddComponent(buildingEntity, GetSelectableTypeComponent(authoring));
            }

            private SelectableElementTypeComponent GetSelectableTypeComponent(BuildingAuthoring authoring)
            {
                return new SelectableElementTypeComponent
                {
                    Type = authoring.SelectableType
                };
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