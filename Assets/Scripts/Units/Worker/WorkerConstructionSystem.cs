using Buildings;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Units.Worker
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [BurstCompile]
    public partial struct WorkerConstructionSystem : ISystem
    {
        private const float CONSTRUCTION_PROGRESS_PER_SECOND = 0.1f;
        
        private const float CONSTRUCTION_DISTANCE_THRESHOLD = 3.0f;
        
        private const float MAX_CONSTRUCTION_VALUE = 1.0f; 
        
        private ComponentLookup<BuildingConstructionProgressComponent> _constructionProgressLookup;
        
        private EntityCommandBuffer _entityCommandBuffer;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _constructionProgressLookup = state.GetComponentLookup<BuildingConstructionProgressComponent>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            _constructionProgressLookup.Update(ref state);
            
            float deltaTime = SystemAPI.Time.DeltaTime;

            foreach ((RefRO<LocalTransform> workerTransform, RefRO<WorkerConstructionTagComponent> constructionTag,
                     Entity workerEntity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<WorkerConstructionTagComponent>>()
                         .WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                ProcessConstruction(workerTransform.ValueRO, constructionTag.ValueRO, 
                                  workerEntity, deltaTime, ref state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessConstruction(LocalTransform workerTransform, WorkerConstructionTagComponent constructionTag,
                                        Entity workerEntity,float deltaTime,ref SystemState state)
        {
            Entity buildingEntity = constructionTag.BuildingEntity;

            if (!state.EntityManager.Exists(buildingEntity))
            {
                _entityCommandBuffer.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
                return;
            }

            if (!_constructionProgressLookup.TryGetComponent(buildingEntity, out BuildingConstructionProgressComponent constructionProgress))
            {
                _entityCommandBuffer.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
                return;
            }

            LocalTransform buildingTransform = state.EntityManager.GetComponentData<LocalTransform>(buildingEntity);
            float distanceSq = math.distancesq(workerTransform.Position, buildingTransform.Position);

            if (distanceSq > CONSTRUCTION_DISTANCE_THRESHOLD * CONSTRUCTION_DISTANCE_THRESHOLD)
            {
                return;
            }

            constructionProgress.Value += CONSTRUCTION_PROGRESS_PER_SECOND * deltaTime;

            if (constructionProgress.Value >= MAX_CONSTRUCTION_VALUE)
            {
                _entityCommandBuffer.RemoveComponent<BuildingConstructionProgressComponent>(buildingEntity);
                _entityCommandBuffer.RemoveComponent<WorkerConstructionTagComponent>(workerEntity);
            }
            else
            {
                _entityCommandBuffer.SetComponent(buildingEntity, constructionProgress);
            }
        }
    }
}

