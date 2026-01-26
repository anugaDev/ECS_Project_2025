using ElementCommons;
using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(GhostReceiveSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial struct DetectPlayerResourceChangesSystem : ISystem
    {

        public void OnUpdate(ref SystemState state)
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<CurrentPopulationComponent> population, Entity playerEntity) in
                     SystemAPI.Query<RefRO<CurrentPopulationComponent>>()
                         .WithAll<PlayerTagComponent, OwnerTagComponent>()
                         .WithChangeFilter<CurrentPopulationComponent>()
                         .WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }

            foreach ((RefRO<CurrentFoodComponent> food, Entity playerEntity) in
                     SystemAPI.Query<RefRO<CurrentFoodComponent>>()
                         .WithAll<PlayerTagComponent, OwnerTagComponent>()
                         .WithChangeFilter<CurrentFoodComponent>()
                         .WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }

            foreach ((RefRO<CurrentWoodComponent> wood, Entity playerEntity) in
                     SystemAPI.Query<RefRO<CurrentWoodComponent>>()
                         .WithAll<PlayerTagComponent, OwnerTagComponent>()
                         .WithChangeFilter<CurrentWoodComponent>()
                         .WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }

            foreach ((RefRO<FoodGenerationComponent> foodGen, Entity playerEntity) in
                     SystemAPI.Query<RefRO<FoodGenerationComponent>>()
                         .WithAll<PlayerTagComponent, OwnerTagComponent>()
                         .WithChangeFilter<FoodGenerationComponent>()
                         .WithEntityAccess())
            {
                entityCommandBuffer.AddComponent<UpdateResourcesPanelTag>(playerEntity);
            }

            entityCommandBuffer.Playback(state.EntityManager);
            entityCommandBuffer.Dispose();
        }
    }
}

