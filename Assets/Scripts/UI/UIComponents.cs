using System.Collections.Generic;
using Buildings;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor.Build;
using UnityEngine;

namespace UI
{
    public class HealthBarUIReferenceComponent : ICleanupComponentData
    {
        public UnitUIController Value;
    }

    public struct HealthBarOffsetComponent : IComponentData
    {
        public float3 Value;
    }

    public struct PlayerUIActionsTagComponent : IComponentData
    {
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
    
    
}