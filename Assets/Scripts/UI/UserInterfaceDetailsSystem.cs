using Combat;
using ElementCommons;
using GatherableResources;
using UI.UIControllers;
using Units.Worker;
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

        private bool _isSelecting; 

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
                _isSelecting = true;
                _selectionDetailsController.EnableDetails();
                SetTrackedEntityDetails();
                entityCommandBuffer.RemoveComponent<SetUIDisplayDetailsComponent>(entity);
            }
            

            foreach ((SetEmptyDetailsComponent _, Entity entity) 
                     in SystemAPI.Query<SetEmptyDetailsComponent>().WithEntityAccess())
            {
                _selectionDetailsController.DisableDetails();
                _isSelecting = false;
                entityCommandBuffer.RemoveComponent<SetEmptyDetailsComponent>(entity);
            }
            
            foreach ((SelectableElementTypeComponent _, Entity entity) 
                     in SystemAPI.Query<SelectableElementTypeComponent>().WithEntityAccess())
            {
                if(entity != _trackedEntity && !_isSelecting)
                {
                    continue;
                }

                SetTrackedEntityDetails();
                entityCommandBuffer.RemoveComponent<UpdateResourcesPanelTag>(entity);
            }

            entityCommandBuffer.Playback(EntityManager);
        }

        private void SetTrackedEntityDetails()
        {
            SetName();
            SetHitPoints();
            SetResources();
        }

        private void SetResources()
        {
            if (!EntityManager.Exists(_trackedEntity) ||
                !EntityManager.HasComponent<CurrentWorkerResourceQuantityComponent>(_trackedEntity))
            {
                _selectionDetailsController.DisableResources();
                return;
            }

            CurrentWorkerResourceQuantityComponent resourceComponent =
                EntityManager.GetComponentData<CurrentWorkerResourceQuantityComponent>(_trackedEntity);

            int value = resourceComponent.Value;
            EnableResources(value);
            _selectionDetailsController.SetResourcesText(value.ToString());

            // Debug log to verify replication
            //UnityEngine.Debug.Log($"[UI-DETAILS] Worker {_trackedEntity.Index} resource value: {value} (ResourceType: {resourceComponent.ResourceType})");
        }

        private void EnableResources(int value)
        {
            if(value <= 0)
            {
                _selectionDetailsController.DisableResources();
            }
            else
            {
                _selectionDetailsController.EnableResources();
            }
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