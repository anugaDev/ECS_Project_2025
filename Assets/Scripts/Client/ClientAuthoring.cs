using Combat;
using Unity.Entities;
using UnityEngine;

namespace Client
{
    public class ClientAuthoring : MonoBehaviour
    {
        public class ClientBaker: Baker<HitPointAuthoring>
        {
            public override void Bake(HitPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<SelectedPositionComponent>(entity);
            }
        }
    }
}