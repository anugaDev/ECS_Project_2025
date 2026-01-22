using ScriptableObjects;
using Types;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Buildings
{
    public struct BuildingComponents : IComponentData
    {
    }

    public struct NewBuildingTagComponent : IComponentData
    {
    }

    public struct BuildingObstacleSizeComponent : IComponentData
    {
        public float3 Size;
    }
    
    public struct BuildingTypeComponent : IComponentData
    {
        public BuildingType Type;
    }

    public struct PlaceBuildingCommand : ICommandData
    {
        public BuildingType BuildingType;

        public float3 Position;
        public NetworkTick Tick { get; set; }
    }
    
    public struct SpawnUnitCommand : ICommandData
    {
        public UnitType UnitType;

        public float3 BuildingPosition;

        public NetworkTick Tick { get; set; }
    }


    public struct BuildingPrefabComponent : IComponentData
    {
        public Entity TownCenter;

        public Entity Barracks;

        public Entity House;

        public Entity Farm;
        
        public Entity Tower;
    }
    
    public class BuildingsConfigurationComponent : IComponentData
    {
        public BuildingsScriptableObject Configuration;
    }
    
    public class BuildingViewReferenceComponent : IComponentData
    {
        public BuildingView Value;
    }

    public class BuildingMaterialsConfigurationComponent : IComponentData
    {
        public BuildingMaterialsConfiguration Configuration;
    }
    
    public struct RecruitmentQueueBufferComponent : IBufferElementData
    {
        public UnitType unitType;
    }
    
    public struct RecruitmentProgressComponent : IComponentData
    {
        public UnitType UnitType;

        public float Value;
    }
}