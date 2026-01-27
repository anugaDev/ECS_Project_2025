using System;
using System.Collections.Generic;
using Buildings;
using GatherableResources;
using ScriptableObjects;
using Types;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Units
{
    [UpdateInGroup(typeof(GhostSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial class QueueUnitCommandServerSystem : SystemBase
    {
        private EntityCommandBuffer _entityCommandBuffer;

        private Dictionary<ResourceType, Action<Entity, int>> _resourceDeductionActions;

        protected override void OnCreate()
        {
            InitializeResourceDeductionActions();

            RequireForUpdate<PlayerTagComponent>();
            RequireForUpdate<NetworkTime>();
            RequireForUpdate<UnitsConfigurationComponent>();

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
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            NetworkTick serverTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

            foreach ((DynamicBuffer<QueueUnitCommand> queueCommands, RefRW<LastProcessedQueueCommand> lastProcessedCommand,
                        Entity playerEntity)
                     in SystemAPI.Query<DynamicBuffer<QueueUnitCommand>, RefRW<LastProcessedQueueCommand>>()
                               .WithAll<PlayerTagComponent>().WithEntityAccess())
            {
                ProcessQueueCommands(queueCommands, serverTick, lastProcessedCommand, playerEntity);
            }

            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void ProcessQueueCommands(DynamicBuffer<QueueUnitCommand> queueCommands,
            NetworkTick serverTick,
            RefRW<LastProcessedQueueCommand> lastProcessedCommand, Entity playerEntity)
        {
            queueCommands.GetDataAtTick(serverTick, out QueueUnitCommand command);

            if (!command.Tick.IsValid)
                return;

            if (command.CommandId == lastProcessedCommand.ValueRO.CommandId)
                return;

            DeductUnitCost(command.UnitType, playerEntity);

            lastProcessedCommand.ValueRW = new LastProcessedQueueCommand()
            {
                CommandId = command.CommandId
            };
        }
        
        private void DeductUnitCost(UnitType unitType, Entity playerEntity)
        {
            UnitsConfigurationComponent config = SystemAPI.ManagedAPI.GetSingleton<UnitsConfigurationComponent>();
            UnitScriptableObject unitConfig = config.Configuration.GetUnitsDictionary()[unitType];

            if (unitConfig == null || unitConfig.RecruitmentCost == null)
            {
                return;
            }

            foreach (ResourceCostEntity cost in unitConfig.RecruitmentCost)
            {
                if (_resourceDeductionActions.TryGetValue(cost.ResourceType, out var deductAction))
                {
                    deductAction(playerEntity, cost.Cost);
                }
            }

            _entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
        }

        private void DeductWood(Entity playerEntity, int cost)
        {
            if (EntityManager.HasComponent<CurrentWoodComponent>(playerEntity))
            {
                CurrentWoodComponent wood = EntityManager.GetComponentData<CurrentWoodComponent>(playerEntity);
                wood.Value -= cost;
                _entityCommandBuffer.SetComponent(playerEntity, wood);
            }
        }

        private void DeductFood(Entity playerEntity, int cost)
        {
            if (EntityManager.HasComponent<CurrentFoodComponent>(playerEntity))
            {
                CurrentFoodComponent food = EntityManager.GetComponentData<CurrentFoodComponent>(playerEntity);
                food.Value -= cost;
                _entityCommandBuffer.SetComponent(playerEntity, food);
            }
        }

        private void DeductPopulation(Entity playerEntity, int cost)
        {
            if (EntityManager.HasComponent<CurrentPopulationComponent>(playerEntity))
            {
                CurrentPopulationComponent population = EntityManager.GetComponentData<CurrentPopulationComponent>(playerEntity);
                population.CurrentPopulation += cost;
                _entityCommandBuffer.SetComponent(playerEntity, population);
            }
        }
    }
}