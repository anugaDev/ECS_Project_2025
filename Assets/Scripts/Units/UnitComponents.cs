using Types;
using Unity.Entities;
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

    /// <summary>
    /// Stores the team materials for a unit. Each unit can have its own materials.
    /// </summary>
    public class UnitMaterialsComponent : IComponentData
    {
        public Material RedTeamMaterial;

        public Material BlueTeamMaterial;
    }
}