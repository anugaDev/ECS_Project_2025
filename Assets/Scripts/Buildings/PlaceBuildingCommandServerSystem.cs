using ElementCommons;
using Types;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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

        public void OnCreate(ref SystemState state)
        {
            _prefabFactory = new BuildingsPrefabEntityFactory();
            state.RequireForUpdate<PlayerTagComponent>();
            state.RequireForUpdate<BuildingPrefabComponent>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            NetworkTick serverTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DynamicBuffer<PlaceBuildingCommand> buildingCommands, RefRW<LastProcessedBuildingCommand> lastProcessedCommand,
                         PlayerTeamComponent playerTeam, GhostOwner ghostOwner)
                     in SystemAPI.Query<DynamicBuffer<PlaceBuildingCommand>, RefRW<LastProcessedBuildingCommand>, PlayerTeamComponent, GhostOwner>()
                               .WithAll<PlayerTagComponent>())
            {
                ProcessBuildingCommands(buildingCommands, serverTick, lastProcessedCommand, playerTeam.Team, ghostOwner.NetworkId, state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void ProcessBuildingCommands(DynamicBuffer<PlaceBuildingCommand> buildingCommands,
            NetworkTick serverTick,
            RefRW<LastProcessedBuildingCommand> lastProcessedCommand, TeamType playerTeam, int networkId,
            SystemState state)
        {
            buildingCommands.GetDataAtTick(serverTick, out PlaceBuildingCommand command);

            if (!command.Tick.IsValid)
                return;

            if (IsDuplicateCommand(command, lastProcessedCommand.ValueRO))
                return;

            lastProcessedCommand.ValueRW = new LastProcessedBuildingCommand
            {
                Tick = command.Tick,
                Position = command.Position,
                BuildingType = command.BuildingType
            };

            InstantiateBuilding(command, playerTeam, networkId, state);
        }

        private bool IsDuplicateCommand(PlaceBuildingCommand newCommand, LastProcessedBuildingCommand lastCommand)
        {
            if (!lastCommand.Tick.IsValid)
                return false;

            bool samePosition = math.distancesq(newCommand.Position, lastCommand.Position) < 0.01f;
            bool sameType = newCommand.BuildingType == lastCommand.BuildingType;

            return samePosition && sameType;
        }

        private void InstantiateBuilding(PlaceBuildingCommand placeBuildingCommand, TeamType playerTeam, int networkId,
            SystemState state)
        {
            Entity buildingEntity = _prefabFactory.Get(placeBuildingCommand.BuildingType);
            Entity newBuilding = _entityCommandBuffer.Instantiate(buildingEntity);

            LocalTransform prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(buildingEntity);
            LocalTransform newTransform = LocalTransform.FromPositionRotationScale(
                placeBuildingCommand.Position,
                prefabTransform.Rotation,
                prefabTransform.Scale);

            _entityCommandBuffer.SetComponent(newBuilding, newTransform);
            _entityCommandBuffer.SetComponent(newBuilding, new GhostOwner{NetworkId = networkId});
            _entityCommandBuffer.SetComponent(newBuilding, new ElementTeamComponent{Team = playerTeam});
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