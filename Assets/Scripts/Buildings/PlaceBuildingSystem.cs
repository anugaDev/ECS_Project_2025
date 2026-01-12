using PlayerInputs;
using Types;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

namespace Buildings
{
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct PlaceBuildingSystem : ISystem
    {
        private BuildingsPrefabEntityFactory _prefabFactory;

        private EntityCommandBuffer _entityCommandBuffer;
        
        private TeamType _currentTeam;

        public void OnCreate(ref SystemState state)
        {
            _currentTeam = TeamType.Red; //REMOVE ON TEAM FIX
            _prefabFactory = new BuildingsPrefabEntityFactory();
            state.RequireForUpdate<OwnerTagComponent>();
            state.RequireForUpdate<BuildingPrefabComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            foreach ((PlaceBuildingRequest buildingRequest, ReceiveRpcCommandRequest requestSource, Entity entity) 
                     in SystemAPI.Query<PlaceBuildingRequest, ReceiveRpcCommandRequest>().WithEntityAccess())
            {
                int clientId = SystemAPI.GetComponent<NetworkId>(requestSource.SourceConnection).Value;
                InstantiateBuilding(buildingRequest, clientId, requestSource);
                _entityCommandBuffer.DestroyEntity(entity);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void InstantiateBuilding(PlaceBuildingRequest buildingRequest, int clientId, ReceiveRpcCommandRequest requestSource)
        {
            Entity buildingEntity = _prefabFactory.Get(buildingRequest.BuildingType);
            Entity newBuilding = _entityCommandBuffer.Instantiate(buildingEntity);
            LinkedEntityGroup linkedEntityGroup = new LinkedEntityGroup();
            linkedEntityGroup.Value = newBuilding; 
            _entityCommandBuffer.SetComponent(newBuilding, LocalTransform.FromPosition(buildingRequest.Position));
            _entityCommandBuffer.SetComponent(newBuilding, new GhostOwner { NetworkId = clientId });
            _entityCommandBuffer.SetComponent(newBuilding, GetTeamComponent());
            _entityCommandBuffer.AppendToBuffer(requestSource.SourceConnection, linkedEntityGroup);
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