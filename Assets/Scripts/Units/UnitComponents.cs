using Types;
using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    public struct UnitTagComponent : IComponentData
    {
    }

    public struct NewUnitTagComponent : IComponentData
    {
    }

    public struct OwnerTagComponent : IComponentData
    {
    }

    public struct EntityTeamComponent : IComponentData
    {
        [GhostField] 
        public TeamType Team;
    }

    public struct UnitMoveSpeedComponent : IComponentData
    {
        public float Speed;
    }

    public struct UnitTypeComponent : IComponentData
    {
        public UnitType Type;
    }

    public struct EntitySelectionComponent : IComponentData
    {
        public bool IsSelected;

        public bool MustUpdateUI;
    }
}