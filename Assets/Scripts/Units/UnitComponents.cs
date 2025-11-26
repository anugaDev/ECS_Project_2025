using Types;
using Unity.Entities;
using Unity.Mathematics;
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
        [GhostField] public TeamType Team;
    }

    public struct UnitMoveSpeedComponent : IComponentData
    {
        public float Speed;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct UnitTargetPositionComponent : IInputComponentData
    {
        [GhostField(Quantization = 0)] public float3 Value;
    }
}