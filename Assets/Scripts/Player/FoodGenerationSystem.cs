using UI;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Player
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
    public partial struct FoodGenerationSystem : ISystem
    {
        private double _lastUpdateTime;
        private const double UPDATE_INTERVAL = 1.0;

        public void OnCreate(ref SystemState state)
        {
            _lastUpdateTime = 0;
        }

        public void OnUpdate(ref SystemState state)
        {
            double currentTime = SystemAPI.Time.ElapsedTime;
            
            if (currentTime - _lastUpdateTime < UPDATE_INTERVAL)
            {
                return;
            }

            _lastUpdateTime = currentTime;
            
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRW<CurrentFoodComponent> currentFood, FoodGenerationComponent foodGeneration, Entity playerEntity)
                     in SystemAPI.Query<RefRW<CurrentFoodComponent>, FoodGenerationComponent>()
                         .WithAll<PlayerTagComponent>().WithEntityAccess())
            {
                if (foodGeneration.FoodPerSecond > 0)
                {
                    currentFood.ValueRW.Value += foodGeneration.FoodPerSecond;
                    ecb.AddComponent<UpdateResourcesPanelTag>(playerEntity);
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
    }
}

