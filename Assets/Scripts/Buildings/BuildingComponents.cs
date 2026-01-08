using Types;
using Unity.Entities;

namespace Buildings
{
    public struct BuildingComponents : IComponentData
    {
    }

    public struct NewBuildingTagComponent : IComponentData
    {
    }
    
    public struct BuildingTypeComponent : IComponentData
    {
        public BuildingType Type;
    }
}