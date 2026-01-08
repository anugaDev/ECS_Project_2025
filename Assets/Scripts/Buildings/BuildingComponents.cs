using ScriptableObjects;
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

    public class BuildingConfigurationComponent : IComponentData
    {
        public BuildingsScriptableObject Configuration;
    }
}