using PlayerInputs;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Buildings
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    public partial struct PlaceBuildingSystem : ISystem
    {
        private BuildingsPrefabEntityFactory _prefabFactory;

        private EntityCommandBuffer _entityCommandBuffer;

        public void OnCreate(ref SystemState state)
        {
            _prefabFactory = new BuildingsPrefabEntityFactory();
            state.RequireForUpdate<OwnerTagComponent>();
            state.RequireForUpdate<BuildingPrefabComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((PlaceBuildingComponent placeBuilding, Entity entity) in SystemAPI.Query<PlaceBuildingComponent>().WithEntityAccess())
            {
                InstantiateBuilding(placeBuilding);
                _entityCommandBuffer.RemoveComponent<PlaceBuildingComponent>(entity);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void InstantiateBuilding(PlaceBuildingComponent placeBuilding)
        {
            Entity buildingEntity = _prefabFactory.Get(placeBuilding.BuildingType);
            Entity newBuilding = _entityCommandBuffer.Instantiate(buildingEntity);
            LinkedEntityGroup linkedEntityGroup = new LinkedEntityGroup();
            linkedEntityGroup.Value = newBuilding;
            _entityCommandBuffer.SetComponent(newBuilding, LocalTransform.FromPosition(placeBuilding.Position));
            //_entityCommandBuffer.AppendToBuffer(requestSource.SourceConnection, linkedEntityGroup);
        }

        private void InitializeFactory()
        {
            if (_prefabFactory.IsInitialized)
            {
                return;
            }

            BuildingPrefabComponent prefabComponent = SystemAPI.GetSingleton<BuildingPrefabComponent>();
            _prefabFactory.Set(prefabComponent);
        }
    }
}