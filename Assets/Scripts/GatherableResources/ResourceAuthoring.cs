using Buildings;
using ElementCommons;
using Types;
using Unity.Entities;
using UnityEngine;

namespace GatherableResources
{
    public class ResourceAuthoring : MonoBehaviour
    {
        [SerializeField] 
        private ResourceType _resourceType;

        [SerializeField]
        private int _maxQuantity;
        
        public ResourceType ResourceType => _resourceType;

        public int MaxQuantity => _maxQuantity;

        public class ResourceBaker : Baker<ResourceAuthoring>
        {
            public override void Bake(ResourceAuthoring authoring)
            {
                Entity resourceEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<ElementSelectionComponent>(resourceEntity);
                AddComponent<ElementDisplayDetailsComponent>(resourceEntity);
                AddComponent(resourceEntity, GetSelectableTypeComponent());
                AddComponent(resourceEntity, GetResourceTypeComponent(authoring));
                AddComponent(resourceEntity, GetMaxResourceQuantityComponent(authoring));
                AddComponent(resourceEntity, GetCurrentResourceQuantityComponent(authoring));
            }

            private MaxResourceQuantityComponent GetMaxResourceQuantityComponent(ResourceAuthoring authoring)
            {
                return new MaxResourceQuantityComponent
                {
                    Value = authoring.MaxQuantity
                };
            }

            private CurrentResourceQuantityComponent GetCurrentResourceQuantityComponent(ResourceAuthoring authoring)
            {
                return new CurrentResourceQuantityComponent
                {
                    Value = authoring.MaxQuantity
                };
            }

            private SelectableElementTypeComponent GetSelectableTypeComponent()
            {
                return new SelectableElementTypeComponent
                {
                    Type = SelectableElementType.Resource
                };
            }

            private ResourceTypeComponent GetResourceTypeComponent(ResourceAuthoring authoring)
            {
                return new ResourceTypeComponent
                {
                    Type = authoring.ResourceType
                };
            }
        }
    }
}