using Combat;
using Unity.Entities;
using UnityEngine;

namespace Client
{
    public class ClientAuthoring : MonoBehaviour
    {
        public class ClientBaker: Baker<ClientAuthoring>
        {
            public override void Bake(ClientAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SelectedPositionComponent>(entity);
            }
        }
    }
}