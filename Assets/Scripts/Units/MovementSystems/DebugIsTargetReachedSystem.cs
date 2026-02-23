// DISABLED: CurrentTargetComponent no longer exists
// This debug system is no longer needed

/*using Units.Worker;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace Units.MovementSystems
{
    /// <summary>
    /// DEBUG SYSTEM: Tracks when IsTargetReached changes value
    /// </summary>
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
    public partial struct DebugIsTargetReachedSystem : ISystem
    {
        private NativeHashMap<Entity, bool> _previousValues;

        public void OnCreate(ref SystemState state)
        {
            _previousValues = new NativeHashMap<Entity, bool>(100, Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            _previousValues.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            foreach ((RefRO<CurrentTargetComponent> currentTarget, Entity entity)
                     in SystemAPI.Query<RefRO<CurrentTargetComponent>>()
                         .WithAll<UnitTagComponent, Simulate>()
                         .WithEntityAccess())
            {
                bool currentValue = currentTarget.ValueRO.IsTargetReached;

                if (_previousValues.TryGetValue(entity, out bool previousValue))
                {
                    if (currentValue != previousValue)
                    {
                        UnityEngine.Debug.LogError($"[DEBUG-REACHED] Entity {entity.Index}: IsTargetReached changed from {previousValue} to {currentValue}!");
                        _previousValues[entity] = currentValue;
                    }
                }
                else
                {
                    // First time seeing this entity
                    _previousValues.Add(entity, currentValue);
                    if (currentValue)
                    {
                        UnityEngine.Debug.LogWarning($"[DEBUG-REACHED] Entity {entity.Index}: INITIALIZED with IsTargetReached=TRUE!");
                    }
                }
            }
        }
    }
}*/

