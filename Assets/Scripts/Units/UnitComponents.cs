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

    public struct UnitTeamComponent : IComponentData
    {
        [GhostField] 
        public TeamType Team;
    }

    public struct UnitMoveSpeedComponent : IComponentData
    {
        public float Speed;
    }

    public struct UnitSelectionComponent : IComponentData
    {
        public bool IsSelected;
    }
}