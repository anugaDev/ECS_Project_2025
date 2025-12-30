using Types;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Client
{
    public struct ClientTeamRequest : IComponentData
    {
        public TeamType Value;
    }
}