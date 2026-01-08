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

    public struct SetBuildingActionComponent : IComponentData
    {
        public BuildingType BuildingType;
    }

    public class BuildingConfigurationComponent : IComponentData
    {
        public BuildingsScriptableObject Configuration;
    }
}