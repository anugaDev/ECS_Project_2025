using Unity.Entities;

namespace PlayerCamera
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class InitializeMainCameraSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<MainCameraTagComponent>();
        }

        protected override void OnUpdate()
        {
            Enabled = false;
            Entity cameraEntity = SystemAPI.GetSingletonEntity<MainCameraTagComponent>();
            EntityManager.SetComponentData(cameraEntity, GetCameraComponentData());
        }

        private MainCameraComponentData GetCameraComponentData()
        {
            return new MainCameraComponentData
            {
                Camera = UnityEngine.Camera.main
            };
        }
    }
}