using Unity.NetCode;

namespace Client
{
    public struct TeamRequest : IRpcCommand
    {
        public TeamType Team;
    }
}