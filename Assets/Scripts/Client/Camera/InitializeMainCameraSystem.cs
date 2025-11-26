using Unity.Entities;
using UnityEngine;

namespace Client
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
                Camera = Camera.main
            };
        }
    }
}