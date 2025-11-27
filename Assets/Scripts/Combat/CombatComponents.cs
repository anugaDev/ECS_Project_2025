using Unity.Entities;
using Unity.NetCode;

namespace Combat
{
    public struct MaxHitPointsComponent : IComponentData
    {
        public int Value;
    }

    public struct CurrentHitPointsComponent : IComponentData
    {
        [GhostField] public int Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct DamageBufferElement : IBufferElementData
    {
        public int Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
    public struct CurrentTickDamageCommand : ICommandData
    {
        public NetworkTick Tick { get; set; }

        public int Value;
    }
}