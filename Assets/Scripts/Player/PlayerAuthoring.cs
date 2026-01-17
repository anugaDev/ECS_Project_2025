using Buildings;
using ElementCommons;
using UI;
using Unity.Entities;
using UnityEngine;

namespace Player
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring playerAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddBuffer<UpdateUIActionPayload>(entity);
                AddBuffer<PlaceBuildingCommand>(entity);
                AddBuffer<SpawnUnitCommand>(entity);
                AddComponent<OwnerTagComponent>(entity);
                AddComponent<PlayerTeamComponent>(entity);
                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}