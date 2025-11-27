using Unity.Entities;
using UnityEngine;

namespace PlayerCamera
{
    public class MainCameraAuthoring : MonoBehaviour
    {
        public class MainCameraBaker : Baker<MainCameraAuthoring>
        {
            public override void Bake(MainCameraAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponentObject(entity, new MainCameraComponentData());
                AddComponent<MainCameraTagComponent>(entity);
            }
        }
    }
}