using Combat;
using ElementCommons;
using UI.UIControllers;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceDetailsSystem : SystemBase
    {
        private SelectedDetailsDisplayController _selectionDetailsController;

        private Entity _trackedEntity;

        protected override void OnCreate()
        {
            RequireForUpdate<OwnerTagComponent>();
        }

        protected override void OnStartRunning()
        {
            _selectionDetailsController = UserInterfaceController.Instance.SelectedDetailsController;
            base.OnStartRunning();
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((SetUIDisplayDetailsComponent detailsComponent, Entity entity) 
                     in SystemAPI.Query<SetUIDisplayDetailsComponent>().WithEntityAccess())
            {
                _trackedEntity = detailsComponent.Entity;
                SetTrackedEntityDetails();
                entityCommandBuffer.RemoveComponent<SetUIDisplayDetailsComponent>(entity);
            }
            foreach ((SetEmptyDetailsComponent _, Entity entity) 
                     in SystemAPI.Query<SetEmptyDetailsComponent>().WithEntityAccess())
            {
                _selectionDetailsController.DisableDetails();
                entityCommandBuffer.RemoveComponent<SetEmptyDetailsComponent>(entity);
            }

            entityCommandBuffer.Playback(EntityManager);
        }

        private void SetTrackedEntityDetails()
        {
            _selectionDetailsController.EnableDetails();
            SetName();
            SetHitPoints();
        }

        private void SetHitPoints()
        {
            int currentHitPoints = EntityManager.GetComponentData<CurrentHitPointsComponent>(_trackedEntity).Value;
            int maxHitPoints = EntityManager.GetComponentData<MaxHitPointsComponent>(_trackedEntity).Value;
            _selectionDetailsController.UpdateHitPoints(currentHitPoints, maxHitPoints);
        }

        private void SetName()           
        {
            EntityManager.GetComponentData<ElementDisplayDetailsComponent>(_trackedEntity);
            ElementDisplayDetailsComponent details = EntityManager.GetComponentData<ElementDisplayDetailsComponent>(_trackedEntity);
            _selectionDetailsController.SetName(details.Name);
            _selectionDetailsController.SetImage(details.Sprite);
        }
    }
}