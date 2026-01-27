using ElementCommons;
using PlayerInputs;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Units.MovementSystems
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateAfter(typeof(Navigation.NavMeshPathfindingSystem))]
    [UpdateBefore(typeof(UnitMoveSystem))]
    public partial struct UnitTargetTrackingSystem : ISystem
    {
        private ComponentLookup<LocalTransform> _transformLookup;
        private ComponentLookup<PhysicsCollider> _colliderLookup;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _transformLookup = state.GetComponentLookup<LocalTransform>(true);
            _colliderLookup = state.GetComponentLookup<PhysicsCollider>(true);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _transformLookup.Update(ref state);
            _colliderLookup.Update(ref state);

            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);

            foreach ((RefRO<LocalTransform> unitTransform, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<UnitSelectedTargetComponent> selectedTarget,
                     RefRW<PathComponent> pathComponent, RefRO<ElementSelectionComponent> selection, Entity unitEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitTargetPositionComponent>, RefRW<UnitSelectedTargetComponent>,
                                       RefRW<PathComponent>, RefRO<ElementSelectionComponent>>()
                         .WithAll<Simulate, UnitTagComponent>().WithEntityAccess())
            {
                if (!selectedTarget.ValueRO.IsFollowingTarget || !pathComponent.ValueRO.HasPath)
                {
                    continue;
                }

                Entity targetEntity = selectedTarget.ValueRO.TargetEntity;

                if (targetEntity == Entity.Null || !_transformLookup.HasComponent(targetEntity))
                {
                    selectedTarget.ValueRW.IsFollowingTarget = false;
                    selectedTarget.ValueRW.TargetEntity = Entity.Null;
                    continue;
                }

                LocalTransform targetTransform = _transformLookup[targetEntity];
                float3 targetPos = targetTransform.Position;

                float3 closestPoint = targetPos;
                if (_colliderLookup.HasComponent(targetEntity))
                {
                    PhysicsCollider targetCollider = _colliderLookup[targetEntity];
                    closestPoint = GetClosestPointOnBounds(unitTransform.ValueRO.Position, 
                                                          targetTransform, 
                                                          targetCollider);
                }

                float distanceToNewTarget = math.distance(targetPosition.ValueRO.Value, closestPoint);
                if (distanceToNewTarget > 0.5f)
                {
                    targetPosition.ValueRW.Value = closestPoint;

                    pathComponent.ValueRW.HasPath = false;
                    pathComponent.ValueRW.CurrentWaypointIndex = 0;
                }
            }

            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }

        [BurstCompile]
        private float3 GetClosestPointOnBounds(float3 unitPosition, LocalTransform targetTransform, PhysicsCollider targetCollider)
        {
            RigidTransform rigidTransform = new RigidTransform(targetTransform.Rotation, targetTransform.Position);
            Aabb aabb = targetCollider.Value.Value.CalculateAabb(rigidTransform);

            float3 closestPoint = new float3(
                math.clamp(unitPosition.x, aabb.Min.x, aabb.Max.x),
                math.clamp(unitPosition.y, aabb.Min.y, aabb.Max.y),
                math.clamp(unitPosition.z, aabb.Min.z, aabb.Max.z)
            );

            return closestPoint;
        }
    }
}

