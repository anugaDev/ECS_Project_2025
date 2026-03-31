using Combat;
using ScriptableObjects;
using Types;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Buildings
{
    public struct BuildingPassivePendingTag : IComponentData
    {
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(InitializeBuildingSystem))]
    public partial struct ApplyBuildingPassiveActionsSystem : ISystem
    {
        private const int MAX_POPULATION_INCREASE = 5;

        private ComponentLookup<CurrentPopulationComponent> _populationLookup;

        private ComponentLookup<FoodGenerationComponent> _foodGenerationLookup;

        private ComponentLookup<BuildingConstructionProgressComponent> _constructionProgressLookup;

        public void OnCreate(ref SystemState state)
        {
            _populationLookup = state.GetComponentLookup<CurrentPopulationComponent>(false);
            _foodGenerationLookup = state.GetComponentLookup<FoodGenerationComponent>(false);
            _constructionProgressLookup = state.GetComponentLookup<BuildingConstructionProgressComponent>(true);
            state.RequireForUpdate<BuildingsConfigurationComponent>();
        }

        public void OnUpdate(ref SystemState state)
        {
            _populationLookup.Update(ref state);
            _foodGenerationLookup.Update(ref state);
            _constructionProgressLookup.Update(ref state);

            BuildingsScriptableObject buildingsConfig = SystemAPI.ManagedAPI.GetSingleton<BuildingsConfigurationComponent>().Configuration;

            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach ((BuildingTypeComponent buildingType, GhostOwner ghostOwner, Entity buildingEntity)
                     in SystemAPI.Query<BuildingTypeComponent, GhostOwner>()
                         .WithAll<NewBuildingTagComponent>().WithEntityAccess())
            {
                bool isUnderConstruction = false;
                if (_constructionProgressLookup.TryGetComponent(buildingEntity,
                        out BuildingConstructionProgressComponent progress))
                {
                    isUnderConstruction = progress.ConstructionTime > 0 && progress.Value < progress.ConstructionTime;
                }

                if (isUnderConstruction)
                {
                    entityCommandBuffer.AddComponent<BuildingPassivePendingTag>(buildingEntity);
                }
                else
                {
                    ApplyPassiveAction(buildingType.Type, buildingEntity, entityCommandBuffer, ghostOwner, buildingsConfig, ref state);
                }

                entityCommandBuffer.RemoveComponent<NewBuildingTagComponent>(buildingEntity);
            }

            foreach ((BuildingTypeComponent buildingType, GhostOwner ghostOwner, Entity buildingEntity)
                     in SystemAPI.Query<BuildingTypeComponent, GhostOwner>()
                         .WithAll<BuildingPassivePendingTag>().WithEntityAccess())
            {
                if (!_constructionProgressLookup.TryGetComponent(buildingEntity,
                        out BuildingConstructionProgressComponent progress))
                    continue;

                if (progress.ConstructionTime > 0 && progress.Value >= progress.ConstructionTime)
                {
                    ApplyPassiveAction(buildingType.Type, buildingEntity, entityCommandBuffer, ghostOwner, buildingsConfig, ref state);
                    entityCommandBuffer.RemoveComponent<BuildingPassivePendingTag>(buildingEntity);
                }
            }

            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        private void ApplyPassiveAction(BuildingType buildingType, Entity buildingEntity,
            EntityCommandBuffer entityCommandBuffer,
            GhostOwner ghostOwner, BuildingsScriptableObject config, ref SystemState state)
        {
            switch (buildingType)
            {
                case BuildingType.House:
                    ApplyHousePassiveAction(entityCommandBuffer, ghostOwner, ref state);
                    break;
                case BuildingType.Farm:
                    ApplyFarmPassiveAction(entityCommandBuffer, ghostOwner, ref state);
                    break;
                case BuildingType.Tower:
                    ApplyTowerPassiveAction(buildingEntity, entityCommandBuffer, config, ref state);
                    break;
            }
        }

        private void ApplyHousePassiveAction(EntityCommandBuffer ecb, GhostOwner ghostOwner, ref SystemState state)
        {
            Entity playerEntity = GetPlayerEntity(ghostOwner.NetworkId, ref state);

            if (playerEntity == Entity.Null)
            {
                return;
            }

            if (_populationLookup.TryGetComponent(playerEntity, out CurrentPopulationComponent population))
            {
                population.MaxPopulation += MAX_POPULATION_INCREASE;
                ecb.SetComponent(playerEntity, population);
                ecb.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }
        }

        private void ApplyFarmPassiveAction(EntityCommandBuffer ecb, GhostOwner ghostOwner, ref SystemState state)
        {
            Entity playerEntity = GetPlayerEntity(ghostOwner.NetworkId, ref state);

            if (playerEntity == Entity.Null)
            {
                return;
            }

            if (_foodGenerationLookup.TryGetComponent(playerEntity, out FoodGenerationComponent foodGeneration))
            {
                foodGeneration.FoodPerSecond += 1;
                ecb.SetComponent(playerEntity, foodGeneration);
                ecb.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }
        }

        private Entity GetPlayerEntity(int networkId, ref SystemState state)
        {
            foreach ((GhostOwner ghostOwner, Entity entity) in
                     SystemAPI.Query<GhostOwner>().WithAll<PlayerTagComponent>().WithEntityAccess())
            {
                if (ghostOwner.NetworkId != networkId)
                {
                    continue;
                }

                return entity;
            }

            return Entity.Null;
        }

        private void ApplyTowerPassiveAction(Entity buildingEntity, EntityCommandBuffer ecb, BuildingsScriptableObject config, ref SystemState state)
        {
            if (!SystemAPI.HasComponent<TowerAttackProperties>(buildingEntity))
            {
                ecb.AddComponent(buildingEntity, new TowerAttackProperties
                {
                    AttackRange = config.TowerAttackRange,
                    DamagePerSecond = config.TowerDamagePerSecond,
                    TargetEntity = Entity.Null
                });
            }
        }
    }
}