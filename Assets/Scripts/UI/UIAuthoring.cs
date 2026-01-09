using PlayerInputs.MoveIndicator;
using Unity.Entities;
using UnityEngine;

namespace UI
{
    public class UIAuthoring : MonoBehaviour
    {
        public class UIBaker : Baker<UIAuthoring>
        {
            public override void Bake(UIAuthoring uiAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddBuffer<UpdateUIActionPayload>(entity);
                AddComponent(entity, new PlayerUIActionsTagComponent());
            }
        }
    }
}