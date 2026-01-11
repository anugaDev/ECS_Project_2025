using Buildings;
using ScriptableObjects;
using UI.UIControllers;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceActionsSystem : SystemBase
    {
        private SelectionActionsDisplayController _selectionActionsController;

        protected override void OnCreate()
        {
            RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            RequireForUpdate<BuildingsConfigurationComponent>();
            RequireForUpdate<UnitsConfigurationComponent>();
        }

        protected override void OnStartRunning()
        {
            _selectionActionsController = UserInterfaceController.Instance.SelectionActionsDisplayerController;
            _selectionActionsController.OnActionSelected += SetPlayerUIActionComponent;
            SetBuildingActions();
            SetRecruitmentActions();
            base.OnStartRunning();
        }

        private void SetRecruitmentActions()
        {
            UnitsScriptableObject configuration = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>().Configuration;
            _selectionActionsController.SetRecruitmentActions(configuration);
        }

        private void SetBuildingActions()
        {
            BuildingsScriptableObject configuration = SystemAPI.ManagedAPI.GetSingleton<BuildingsConfigurationComponent>().Configuration;
            _selectionActionsController.SetBuildingActions(configuration);
        }

        private void SetPlayerUIActionComponent(SetPlayerUIActionComponent actionComponent)
        {
            Entity uiEntity = SystemAPI.GetSingletonEntity<PlayerUIActionsTagComponent>();
            EntityManager.AddComponentData(uiEntity, actionComponent);
        }

        protected override void OnStopRunning()
        {
            _selectionActionsController.OnActionSelected -= SetPlayerUIActionComponent;
            base.OnStopRunning();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((EnableUIActionComponent enableComponent, Entity entity) 
                in SystemAPI.Query<EnableUIActionComponent>().WithEntityAccess())
            {
                _selectionActionsController.EnableAction(enableComponent);
                entityCommandBuffer.RemoveComponent<EnableUIActionComponent>(entity);
            }

            foreach ((DisableUIActionComponent disableComponent, Entity entity) 
                     in SystemAPI.Query<DisableUIActionComponent>().WithEntityAccess())
            {
                _selectionActionsController.DisableAction(disableComponent);
                entityCommandBuffer.RemoveComponent<DisableUIActionComponent>(entity);
            }

            foreach ((RefRO<UpdateUIActionTag> updateUIActionTag, DynamicBuffer<UpdateUIActionPayload> buffer, Entity entity) in
                     SystemAPI.Query<RefRO<UpdateUIActionTag>, DynamicBuffer<UpdateUIActionPayload>>()
                         .WithEntityAccess())
            {
                _selectionActionsController.SetActionsActive(buffer);
                buffer.Clear();
                entityCommandBuffer.RemoveComponent<UpdateUIActionTag>(entity);
            }

            entityCommandBuffer.Playback(EntityManager);
        }
    }
}