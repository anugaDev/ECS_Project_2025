using System;
using System.Collections.Generic;
using ElementCommons;
using GatherableResources;
using ScriptableObjects;
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
    public partial class PlaceBuildingCommandServerSystem : SystemBase
    {
        private BuildingsPrefabEntityFactory _prefabFactory;

        private EntityCommandBuffer _entityCommandBuffer;

        private Dictionary<ResourceType, Action<Entity, int>> _resourceDeductionActions;

        protected override void OnCreate()
        {
            _prefabFactory = new BuildingsPrefabEntityFactory();

            InitializeResourceDeductionActions();

            RequireForUpdate<PlayerTagComponent>();
            RequireForUpdate<BuildingPrefabComponent>();
            RequireForUpdate<NetworkTime>();
            RequireForUpdate<BuildingsConfigurationComponent>();

            base.OnCreate();
        }

        private void InitializeResourceDeductionActions()
        {
            _resourceDeductionActions = new Dictionary<ResourceType, Action<Entity, int>>
            {
                [ResourceType.Wood] = DeductWood,
                [ResourceType.Food] = DeductFood,
                [ResourceType.Population] = DeductPopulation
            };
        }

        protected override void OnUpdate()
        {
            InitializeFactory();
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            NetworkTick serverTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DynamicBuffer<PlaceBuildingCommand> buildingCommands, RefRW<LastProcessedBuildingCommand> lastProcessedCommand,
                         PlayerTeamComponent playerTeam, GhostOwner ghostOwner, Entity playerEntity)
                     in SystemAPI.Query<DynamicBuffer<PlaceBuildingCommand>, RefRW<LastProcessedBuildingCommand>, PlayerTeamComponent, GhostOwner>()
                               .WithAll<PlayerTagComponent>().WithEntityAccess())
            {
                ProcessBuildingCommands(buildingCommands, serverTick, lastProcessedCommand, playerTeam.Team, ghostOwner.NetworkId, playerEntity);
            }

            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessBuildingCommands(DynamicBuffer<PlaceBuildingCommand> buildingCommands,
            NetworkTick serverTick,
            RefRW<LastProcessedBuildingCommand> lastProcessedCommand, TeamType playerTeam, int networkId,
            Entity playerEntity)
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

            DeductBuildingCost(command.BuildingType, playerEntity);
            InstantiateBuilding(command, playerTeam, networkId);
        }

        private bool IsDuplicateCommand(PlaceBuildingCommand newCommand, LastProcessedBuildingCommand lastCommand)
        {
            if (!lastCommand.Tick.IsValid)
                return false;

            bool samePosition = math.distancesq(newCommand.Position, lastCommand.Position) < 0.01f;
            bool sameType = newCommand.BuildingType == lastCommand.BuildingType;

            return samePosition && sameType;
        }

        private void InstantiateBuilding(PlaceBuildingCommand placeBuildingCommand, TeamType playerTeam, int networkId)
        {
            Entity buildingEntity = _prefabFactory.Get(placeBuildingCommand.BuildingType);
            Entity newBuilding = _entityCommandBuffer.Instantiate(buildingEntity);

            LocalTransform prefabTransform = EntityManager.GetComponentData<LocalTransform>(buildingEntity);
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

        private void DeductBuildingCost(BuildingType buildingType, Entity playerEntity)
        {
            BuildingsConfigurationComponent config = SystemAPI.ManagedAPI.GetSingleton<BuildingsConfigurationComponent>();
            BuildingScriptableObject buildingConfig = config.Configuration.GetBuildingsDictionary()[buildingType];

            if (buildingConfig == null || buildingConfig.ConstructionCost == null)
            {
                return;
            }

            DeductCosts(playerEntity, buildingConfig);
        }

        private void DeductCosts(Entity playerEntity, BuildingScriptableObject buildingConfig)
        {
            foreach (var cost in buildingConfig.ConstructionCost)
            {
                SetDeductAction(playerEntity, cost);
            }

            _entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
        }

        private void SetDeductAction(Entity playerEntity, ResourceCostEntity cost)
        {
            if (!_resourceDeductionActions.TryGetValue(cost.ResourceType, out var deductAction))
            {
                return;
            }

            deductAction(playerEntity, cost.Cost);
        }

        private void DeductWood(Entity playerEntity, int cost)
        {
            if (!EntityManager.HasComponent<CurrentWoodComponent>(playerEntity))
            {
                return;
            }

            CurrentWoodComponent wood = EntityManager.GetComponentData<CurrentWoodComponent>(playerEntity);
            wood.Value -= cost;
            _entityCommandBuffer.SetComponent(playerEntity, wood);
        }

        private void DeductFood(Entity playerEntity, int cost)
        {
            if (!EntityManager.HasComponent<CurrentFoodComponent>(playerEntity))
            {
                return;
            }

            CurrentFoodComponent food = EntityManager.GetComponentData<CurrentFoodComponent>(playerEntity);
            food.Value -= cost;
            _entityCommandBuffer.SetComponent(playerEntity, food);
        }

        private void DeductPopulation(Entity playerEntity, int cost)
        {
            if (!EntityManager.HasComponent<CurrentPopulationComponent>(playerEntity))
            {
                return;
            }

            CurrentPopulationComponent population = EntityManager.GetComponentData<CurrentPopulationComponent>(playerEntity);
            population.CurrentPopulation += cost;
            _entityCommandBuffer.SetComponent(playerEntity, population);
        }
    }
}