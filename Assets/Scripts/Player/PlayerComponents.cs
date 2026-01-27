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

    public struct EnableUIActionBuffer : IBufferElementData
    {
        public PlayerUIActionType Action;

        public int PayloadID;
    }

    public struct DisableUIActionBuffer : IBufferElementData
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
    public struct ValidateUIActionsTag : IComponentData
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
        public int CommandId;
    }

    public struct LastProcessedQueueCommand : IComponentData
    {
        public int CommandId;
    }

    public struct SetEmptyDetailsComponent : IComponentData
    {
    }

    public struct UpdateResourcesPanelTag : IComponentData
    {
    }
    
    public struct CurrentPopulationComponent : IComponentData
    {
        [GhostField]
        public int MaxPopulation;

        [GhostField]
        public int CurrentPopulation;
    }

    public struct CurrentWoodComponent : IComponentData
    {
        [GhostField]
        public int Value;
    }

    public struct CurrentFoodComponent : IComponentData
    {
        [GhostField]
        public int Value;
    }

    public struct FoodGenerationComponent : IComponentData
    {
        [GhostField]
        public int FoodPerSecond;
    }
}