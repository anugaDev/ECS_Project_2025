using Buildings;
using ElementCommons;
using Types;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Units
{
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct SpawnUnitCommandServerSystem : ISystem
    {
        private const float MAX_SPAWN_DISTANCE = 7F;

        private const float MIN_SPAWN_DISTANCE = 5F;

        private const float EXTENTS_MULTIPLIER = 2f;
        
        private UnitsPrefabEntityFactory _prefabFactory;

        private EntityCommandBuffer _entityCommandBuffer;

        private Random _random;

        public void OnCreate(ref SystemState state)
        {
            _prefabFactory = new UnitsPrefabEntityFactory();
            _random = new Random((uint)System.DateTime.Now.Ticks);
            state.RequireForUpdate<PlayerTagComponent>();
            state.RequireForUpdate<UnitPrefabComponent>();
            state.RequireForUpdate<NetworkTime>();
        }

        public void OnUpdate(ref SystemState state)
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            NetworkTick serverTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DynamicBuffer<SpawnUnitCommand> spawnUnitCommands, RefRW<LastProcessedUnitCommand> lastProcessedCommand,
                         PlayerTeamComponent playerTeam, GhostOwner ghostOwner)
                     in SystemAPI.Query<DynamicBuffer<SpawnUnitCommand>, RefRW<LastProcessedUnitCommand>, PlayerTeamComponent, GhostOwner>()
                               .WithAll<PlayerTagComponent>())
            {
                ProcessSpawnCommands(spawnUnitCommands, serverTick, lastProcessedCommand, playerTeam.Team, ghostOwner.NetworkId, state);
            }

            _entityCommandBuffer.Playback(state.EntityManager);
        }

        private void ProcessSpawnCommands(DynamicBuffer<SpawnUnitCommand> spawnUnitCommands,
            NetworkTick serverTick,
            RefRW<LastProcessedUnitCommand> lastProcessedCommand, TeamType playerTeam, int networkId, SystemState state)
        {
            spawnUnitCommands.GetDataAtTick(serverTick, out SpawnUnitCommand command);

            if (!command.Tick.IsValid)
                return;

            if (IsDuplicateCommand(command, lastProcessedCommand.ValueRO))
                return;

            lastProcessedCommand.ValueRW = new LastProcessedUnitCommand()
            {
                Tick = command.Tick,
                BuildingPosition = command.BuildingPosition,
                UnitType = command.UnitType
            };

            InstantiateUnit(command, playerTeam, networkId, state);
        }

        private bool IsDuplicateCommand(SpawnUnitCommand newCommand, LastProcessedUnitCommand lastCommand)
        {
            if (!lastCommand.Tick.IsValid)
                return false;

            bool sameBuilding = math.distancesq(newCommand.BuildingPosition, lastCommand.BuildingPosition) < 0.01f;
            bool sameUnitType = newCommand.UnitType == lastCommand.UnitType;

            if (!sameBuilding || !sameUnitType)
                return false;

            int tickDifference = newCommand.Tick.TicksSince(lastCommand.Tick);
            return tickDifference < 120;
        }

        private void InstantiateUnit(SpawnUnitCommand spawnUnitCommand, TeamType playerTeam, int networkId,
            SystemState state)
        {
            Entity unitPrefab = _prefabFactory.Get(spawnUnitCommand.UnitType);
            Entity newUnit = _entityCommandBuffer.Instantiate(unitPrefab);

            LocalTransform prefabTransform = state.EntityManager.GetComponentData<LocalTransform>(unitPrefab);
            LocalTransform newTransform = LocalTransform.FromPositionRotationScale(
                GetSpawnPosition(spawnUnitCommand.BuildingPosition),
                prefabTransform.Rotation,
                prefabTransform.Scale);

            _entityCommandBuffer.SetComponent(newUnit, newTransform);
            _entityCommandBuffer.SetComponent(newUnit, new GhostOwner{NetworkId = networkId});
            _entityCommandBuffer.SetComponent(newUnit, new ElementTeamComponent{Team = playerTeam});
        }

        private float3 GetSpawnPosition(float3 buildingPosition)
        {
            float angle = _random.NextFloat(0f, math.PI * EXTENTS_MULTIPLIER);
            float distance = _random.NextFloat(MIN_SPAWN_DISTANCE, MAX_SPAWN_DISTANCE);

            float randomXPosition = math.cos(angle) * distance;
            float randomZPosition = math.sin(angle) * distance;
            float3 offset = new float3(randomXPosition, 0f, randomZPosition);

            return buildingPosition + offset;
        }

        private void InitializeFactory()
        {
            if (_prefabFactory.IsInitialized)
            {
                return;
            }

            UnitPrefabComponent prefabComponent = SystemAPI.GetSingleton<UnitPrefabComponent>();
            _prefabFactory.Set(prefabComponent);
        }
    }
}