using Types;
using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    public class UnitComponents
    {
        public struct UnitTagComponent : IComponentData{}
        public struct NewUnitTagComponent : IComponentData{}

        public struct UnitTeam : IComponentData
        {
            [GhostField]
            public TeamType Team;
        }
    }
}