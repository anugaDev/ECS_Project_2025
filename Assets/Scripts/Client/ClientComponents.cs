using Types;
using Unity.Entities;
using Unity.Mathematics;

namespace Client
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }

    public struct SelectedPositionComponent : IComponentData
    {
        public float3 Value;
    }
}