using System.Collections.Generic;
using Buildings;
using Types;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEditor.Build;
using UnityEngine;

namespace UI
{
    public class HealthBarUIReferenceComponent : ICleanupComponentData
    {
        public UnitUIController Value;
    }

    public struct SelectionFeedbackOffset : IComponentData
    {
        public float3 HealthBarOffset;
    }

    public struct PlayerTagComponent : IComponentData
    {
    }

    public struct SetUIDisplayDetailsComponent : IComponentData
    {
        public Entity Entity;
    }

    public struct SetPlayerUIActionComponent : IComponentData
    {
        public PlayerUIActionType Action;

        public int PayloadID;
    }

    public struct EnableUIActionComponent : IComponentData
    {
        public PlayerUIActionType Action;

        public int PayloadID;
    }

    public struct DisableUIActionComponent : IComponentData
    {
        public PlayerUIActionType Action;

        public int PayloadID;
    }

    public struct UpdateUIActionPayload : IBufferElementData
    {
        public PlayerUIActionType Action;
        
        public int PayloadID;
    }

    public struct UpdateUIActionTag : IComponentData
    {
    }

    public struct PlayerPrefabComponent : IComponentData
    {
        public Entity Entity;
    }
    
    public struct PlayerTeamComponent : IComponentData
    {
        [GhostField]
        public TeamType Team;
    }

    public struct LastProcessedBuildingCommand : IComponentData
    {
        public NetworkTick Tick;
        public float3 Position;
        public BuildingType BuildingType;
    }

    public struct LastProcessedUnitCommand : IComponentData
    {
        public NetworkTick Tick;

        public float3 BuildingPosition;

        public UnitType UnitType;
    }
    
    public struct SetEmptyDetailsComponent : IComponentData
    {
    }

    public struct UpdateResourcesPanelTag : IComponentData
    {
    }
    
    public struct CurrentPopulationComponent : IComponentData
    {
        public int MaxPopulation;

        public int CurrentPopulation;
    }

    public struct CurrentWoodComponent : IComponentData
    {
        public int Value;
    }
    
    public struct CurrentFoodComponent : IComponentData
    {
        public int Value;
    }
}