using Buildings;
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
                // PlaceBuildingCommand is ICommandData - NetCode manages this automatically, don't add manually
                AddComponent<PlayerTeamComponent>(entity);
                AddComponent(entity, new PlayerTagComponent());
            }
        }
    }
}