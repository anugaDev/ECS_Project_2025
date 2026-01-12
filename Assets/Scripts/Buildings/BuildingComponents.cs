using ScriptableObjects;
using Types;
using Unity.Entities;
using Unity.Mathematics;

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

    public class BuildingsConfigurationComponent : IComponentData
    {
        public BuildingsScriptableObject Configuration;
    }

    public struct PlaceBuildingComponent : IComponentData
    {
        public BuildingType BuildingType;

        public float3 Position;
    }

    public struct BuildingPrefabComponent : IComponentData
    {
        public Entity TownCenter;

        public Entity Barracks;

        public Entity House;

        public Entity Farm;
    }
}