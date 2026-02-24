using ElementCommons;
using Types;
using Unity.Entities;
using UnityEngine;

namespace Buildings
{
    public class BuildingAuthoring : MonoBehaviour
    {
        [SerializeField] private BuildingType _buildingType;

        [SerializeField] private SelectableElementType _selectableType;

        [SerializeField] private BuildingView _buildingView;

        [SerializeField] private float _constructionTime;
        
        [SerializeField] private GameObject _constructionSite;
        
        [SerializeField] private GameObject _pivot;
        
        public GameObject Pivot => _pivot;

        public BuildingType BuildingType => _buildingType;

        public SelectableElementType SelectableType => _selectableType;

        public BuildingView buildingView => _buildingView;
        
        public float ConstructionTime => _constructionTime;
        
        public GameObject ConstructionSite => _constructionSite;

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
                AddComponent(buildingEntity, GetObstacleSizeComponent(authoring));
                AddComponent(buildingEntity, GetSelectableTypeComponent(authoring));
                AddComponent(buildingEntity, GetBuildingProgressComponent(authoring));
                AddComponent(buildingEntity, GetBuildingViewReferenceComponent(authoring));
            }

            private BuildingConstructionProgressComponent GetBuildingProgressComponent(BuildingAuthoring authoring)
            {
                return new BuildingConstructionProgressComponent
                {
                    ConstructionTime = authoring.ConstructionTime,
                    Value = 0F
                };
            }

            private BuildingPivotReferencesComponent GetBuildingViewReferenceComponent(BuildingAuthoring authoring)
            {
                return new BuildingPivotReferencesComponent
                {
                    ConstructionSiteEntity = authoring.ConstructionSite != null 
                        ? GetEntity(authoring.ConstructionSite, TransformUsageFlags.Dynamic) 
                        : Entity.Null,
                    PivotEntity = authoring.Pivot != null 
                        ? GetEntity(authoring.Pivot, TransformUsageFlags.Dynamic) 
                        : Entity.Null
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
                Vector3 size = new Vector3(5f, 5f, 5f);

                BoxCollider boxCollider = authoring.GetComponent<BoxCollider>();
                if (boxCollider != null)
                {
                    size = boxCollider.size;
                }
                else
                {
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