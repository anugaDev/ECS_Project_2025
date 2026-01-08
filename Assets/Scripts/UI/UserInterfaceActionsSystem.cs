using Buildings;
using ScriptableObjects;
using UI.UIControllers;
using Unity.Entities;
using Unity.Transforms;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceActionsSystem : SystemBase
    {
        private ActionDisplayController _actionController;

        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
            state.RequireForUpdate<BuildingConfigurationComponent>();
        }

        protected override void OnStartRunning()
        {
            _actionController = UserInterfaceController.Instance.ActionDisplayerController;
            SetBuildingActions();
            base.OnStartRunning();
        }

        private void SetBuildingActions()
        {
            BuildingsScriptableObject configuration = SystemAPI.ManagedAPI.GetSingleton<BuildingConfigurationComponent>().Configuration;
            _actionController.SetBuildingActions(configuration);
            _actionController.OnActionSelected += SetActionData;
        }

        private void SetActionData(IComponentData actionComponentData)
        { 
            Entity uiEntity = SystemAPI.GetSingletonEntity<PlayerUIActionsTagComponent>();
            //EntityManager.SetComponentData(uiEntity, actionComponentData);
        }

        protected override void OnStopRunning()
        {
            _actionController.OnActionSelected -= SetActionData;
            base.OnStopRunning();
        }

        protected override void OnUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}