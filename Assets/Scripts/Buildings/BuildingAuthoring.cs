using ElementCommons;
using Types;
using Unity.Entities;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

namespace Buildings
{
    public class BuildingAuthoring : MonoBehaviour
    {
        [SerializeField] private BuildingType _buildingType;

        [SerializeField] private SelectableElementType _selectableType;

        [SerializeField] private BuildingView _buildingView;

        public BuildingType BuildingType => _buildingType;

        public SelectableElementType SelectableType => _selectableType;

        public BuildingView buildingView => _buildingView;

        public class BuildingBaker : Baker<BuildingAuthoring>
        {
            public override void Bake(BuildingAuthoring authoring)
            {
                Entity buildingEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<BuildingComponents>(buildingEntity);
                AddComponent<NewBuildingTagComponent>(buildingEntity);
                AddComponent<ElementTeamComponent>(buildingEntity);
                AddComponent<ElementSelectionComponent>(buildingEntity);
                AddComponent<RecruitmentProgressComponent>(buildingEntity);
                AddBuffer<RecruitmentQueueBufferComponent>(buildingEntity);
                AddComponent<ElementDisplayDetailsComponent>(buildingEntity);
                AddComponent(buildingEntity, GetUnitTypeComponent(authoring));
                AddComponent(buildingEntity, GetSelectableTypeComponent(authoring));
                AddComponent(buildingEntity, GetObstacleSizeComponent(authoring));
                AddComponentObject(buildingEntity, GetBuildingViewReferenceComponent(authoring));
            }

            private BuildingViewReferenceComponent GetBuildingViewReferenceComponent(BuildingAuthoring authoring)
            {
                return new BuildingViewReferenceComponent
                {
                    Value = authoring.buildingView
                };
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

            private BuildingObstacleSizeComponent GetObstacleSizeComponent(BuildingAuthoring authoring)
            {
                // Try to get size from BoxCollider, otherwise use default
                Vector3 size = new Vector3(5f, 5f, 5f); // Default size

                BoxCollider boxCollider = authoring.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    size = boxCollider.size;
                }
                else
                {
                    // Try to find BoxCollider in children (building view)
                    if (authoring.buildingView != null)
                    {
                        BoxCollider childCollider = authoring.buildingView.GetComponentInChildren<BoxCollider>();
                        if (childCollider != null)
                        {
                            size = childCollider.size;
                        }
                    }
                }

                return new BuildingObstacleSizeComponent
                {
                    Size = size
                };
            }
        }
    }
}