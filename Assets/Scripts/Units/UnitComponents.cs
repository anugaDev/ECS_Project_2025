using Types;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace Units
{
    public struct UnitTagComponent : IComponentData
    {
    }

    public struct NewUnitTagComponent : IComponentData
    {
    }

    public struct UnitMoveSpeedComponent : IComponentData
    {
        public float Speed;
    }

    public struct UnitTypeComponent : IComponentData
    {
        public UnitType Type;
    }

    public class UnitMaterialsComponent : IComponentData
    {
        public Material RedTeamMaterial;
        public Material BlueTeamMaterial;
    }
    
    public struct PathComponent : IComponentData
    {
        public float3 LastTargetPosition;

        public int CurrentWaypointIndex;

        public bool HasPath;
    }

    public struct PathWaypointBuffer : IBufferElementData
    {
        public float3 Position;
    }
    
    public struct UnitAttackingTagComponent : IComponentData
    {
        [GhostField]
        public Entity AttackingEntity;
    }
}