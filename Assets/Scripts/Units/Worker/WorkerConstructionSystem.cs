using Buildings;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.Worker
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(MovementSystems.UnitStateSystem))]
    public partial class WorkerConstructionSystem : SystemBase
    {
        private const float CONSTRUCTION_PROGRESS_PER_SECOND = 0.1f;
        private const float CONSTRUCTION_DISTANCE_THRESHOLD  = 3.0f;
        private const float MAX_CONSTRUCTION_VALUE           = 1.0f;

        private ComponentLookup<BuildingConstructionProgressComponent> _constructionProgressLookup;
        private ComponentLookup<LocalTransform>                        _transformLookup;

        protected override void OnCreate()
        {
            _constructionProgressLookup = GetComponentLookup<BuildingConstructionProgressComponent>();
            _transformLookup            = GetComponentLookup<LocalTransform>(true);
            RequireForUpdate<UnitTagComponent>();
        }

        protected override void OnUpdate()
        {
            _constructionProgressLookup.Update(this);
            _transformLookup.Update(this);

            float deltaTime = SystemAPI.Time.DeltaTime;
            var ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<LocalTransform>               workerTransform,
                      RefRO<WorkerConstructionTagComponent> constructionTag,
                      Entity                               workerEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>,
                                        RefRO<WorkerConstructionTagComponent>>()
                         .WithAll<Simulate, UnitTagComponent>()
                         .WithEntityAccess())
            {
                ProcessConstruction(workerTransform.ValueRO, constructionTag.ValueRO,
                                  workerEntity, deltaTime, ref ecb);
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private void ProcessConstruction(LocalTransform workerTransform, WorkerConstructionTagComponent constructionTag,
                                        Entity workerEntity, float deltaTime, ref EntityCommandBuffer ecb)
        {
            Entity buildingEntity = constructionTag.BuildingEntity;

            if (!EntityManager.Exists(buildingEntity))
            {
                ecb.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
                return;
            }

            if (!_constructionProgressLookup.TryGetComponent(buildingEntity, out BuildingConstructionProgressComponent constructionProgress))
            {
                ecb.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
                return;
            }

            if (!_transformLookup.TryGetComponent(buildingEntity, out LocalTransform buildingTransform))
                return;

            float distanceSq = math.distancesq(workerTransform.Position, buildingTransform.Position);
            if (distanceSq > CONSTRUCTION_DISTANCE_THRESHOLD * CONSTRUCTION_DISTANCE_THRESHOLD)
                return;

            constructionProgress.Value += CONSTRUCTION_PROGRESS_PER_SECOND * deltaTime;

            if (constructionProgress.Value >= MAX_CONSTRUCTION_VALUE)
            {
                ecb.RemoveComponent<BuildingConstructionProgressComponent>(buildingEntity);
                ecb.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
            }
            else
            {
                ecb.SetComponent(buildingEntity, constructionProgress);
            }
        }
    }
}
