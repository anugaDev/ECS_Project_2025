using ScriptableObjects;
using Types;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Buildings
{
    public struct BuildingTagComponent : IComponentData
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

    public struct PlaceBuildingRequest : IRpcCommand
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