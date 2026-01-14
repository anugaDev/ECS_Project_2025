using PlayerInputs;
using Types;
using UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Buildings
{
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlaceBuildingCommandServerSystem : ISystem
    {
        private BuildingsPrefabEntityFactory _prefabFactory;

        private EntityCommandBuffer _entityCommandBuffer;
        
        private TeamType _currentTeam;

        public void OnCreate(ref SystemState state)
        {
            _currentTeam = TeamType.Red; //REMOVE ON TEAM FIX
            _prefabFactory = new BuildingsPrefabEntityFactory();
            state.RequireForUpdate<PlayerTagComponent>();
            state.RequireForUpdate<BuildingPrefabComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((DynamicBuffer<PlaceBuildingCommand> bufferBuild, Entity entity) 
                     in SystemAPI.Query<DynamicBuffer<PlaceBuildingCommand>>().WithEntityAccess())
            {
                InstantiateBuildings(bufferBuild);
                _entityCommandBuffer.DestroyEntity(entity);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void InstantiateBuildings(DynamicBuffer<PlaceBuildingCommand> bufferBuild)
        {
            foreach (PlaceBuildingCommand placeBuildingCommand in bufferBuild)
            {
                InstantiateBuilding(placeBuildingCommand);
            }

            bufferBuild.Clear();
        }

        private void InstantiateBuilding(PlaceBuildingCommand placeBuildingCommand)
        {
            Entity buildingEntity = _prefabFactory.Get(placeBuildingCommand.BuildingType);
            Entity newBuilding = _entityCommandBuffer.Instantiate(buildingEntity);
            _entityCommandBuffer.SetComponent(newBuilding, LocalTransform.FromPosition(placeBuildingCommand.Position));
            //_entityCommandBuffer.SetComponent(newBuilding, new GhostOwner{NetworkId = owner.NetworkId});
            _entityCommandBuffer.SetComponent(newBuilding, GetTeamComponent());
        }

        private EntityTeamComponent GetTeamComponent()
        {
            return new EntityTeamComponent
            {
                Team = _currentTeam//TODO Get team
            };
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