using Types;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Buildings
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    [UpdateAfter(typeof(InitializeBuildingSystem))]
    public partial struct ApplyBuildingPassiveActionsSystem : ISystem
    {
        private const int MAX_POPULATION_INCREASE = 5;

        private ComponentLookup<CurrentPopulationComponent> _populationLookup;

        private ComponentLookup<FoodGenerationComponent> _foodGenerationLookup;

        public void OnCreate(ref SystemState state)
        {
            _populationLookup = state.GetComponentLookup<CurrentPopulationComponent>(false);
            _foodGenerationLookup = state.GetComponentLookup<FoodGenerationComponent>(false);
        }

        public void OnUpdate(ref SystemState state)
        {
            _populationLookup.Update(ref state);
            _foodGenerationLookup.Update(ref state);

            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach ((BuildingTypeComponent buildingType, GhostOwner ghostOwner, Entity buildingEntity)
                     in SystemAPI.Query<BuildingTypeComponent, GhostOwner>()
                         .WithAll<NewBuildingTagComponent>().WithEntityAccess())
            {
                ApplyPassiveAction(buildingType.Type, entityCommandBuffer, buildingEntity, ghostOwner, ref state);
                entityCommandBuffer.RemoveComponent<NewBuildingTagComponent>(buildingEntity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }

        private void ApplyPassiveAction(BuildingType buildingType, EntityCommandBuffer entityCommandBuffer, Entity buildingEntity,
                                        GhostOwner ghostOwner, ref SystemState state)
        {
            switch (buildingType)
            {
                case BuildingType.House:
                    ApplyHousePassiveAction(entityCommandBuffer, ghostOwner, ref state);
                    break;
                case BuildingType.Farm:
                    ApplyFarmPassiveAction(entityCommandBuffer, ghostOwner, ref state);
                    break;
                case BuildingType.Center:
                case BuildingType.Barracks:
                case BuildingType.Tower:
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
    }
}

