using ElementCommons;
using PlayerInputs;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AI;

namespace Navigation
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(Units.MovementSystems.UnitMoveSystem))]
    public partial class NavMeshPathfindingSystem : SystemBase
    {
        private NavMeshPath _reusablePath;

        private int _walkableAreaMask;

        protected override void OnCreate()
        {
            _reusablePath = new NavMeshPath();
            _walkableAreaMask = 1 << NavMesh.GetAreaFromName("Walkable");
        }

        protected override void OnUpdate()
        {
            const float TARGET_CHANGE_THRESHOLD = 0.1f;

            foreach ((RefRO<LocalTransform> transform, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRO<ElementSelectionComponent> selection, Entity entity)
                     in SystemAPI.Query<RefRO<LocalTransform>, RefRW<UnitTargetPositionComponent>, RefRW<PathComponent>, DynamicBuffer<PathWaypointBuffer>, RefRO<ElementSelectionComponent>>()
                         .WithAll<Simulate, UnitTagComponent>()
                         .WithEntityAccess())
            {
                UpdateUnitPathfinding(targetPosition, pathComponent, TARGET_CHANGE_THRESHOLD, pathBuffer, transform, selection.ValueRO.IsSelected, entity);
            }
        }

        private void UpdateUnitPathfinding(RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, float TARGET_CHANGE_THRESHOLD,
            DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRO<LocalTransform> transform, bool isSelected, Entity entity)
        {
            if (!targetPosition.ValueRO.MustMove)
            {
                return;
            }

            CheckUnitTarget(targetPosition, pathComponent, TARGET_CHANGE_THRESHOLD, pathBuffer, transform, isSelected, entity);
        }

        private void CheckUnitTarget(RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, float TARGET_CHANGE_THRESHOLD,
            DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRO<LocalTransform> transform, bool isSelected, Entity entity)
        {
            float3 currentTarget = targetPosition.ValueRO.Value;
            bool isTargetChanged = GetIsTargetChanged(pathComponent, TARGET_CHANGE_THRESHOLD, currentTarget);

            if (!isTargetChanged && pathComponent.ValueRO.HasPath)
            {
                return;
            }

            SetUnitPath(targetPosition, pathComponent, pathBuffer, transform, currentTarget, isSelected, entity);
        }

        private void SetUnitPath(RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRO<LocalTransform> transform,
            float3 currentTarget, bool isSelected, Entity entity)
        {
            pathBuffer.Clear();
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathComponent.ValueRW.HasPath = false;

            Vector3 startPos = transform.ValueRO.Position;
            Vector3 endPos = currentTarget;

            SetUnitPathfinding(targetPosition, pathComponent, pathBuffer, startPos, endPos, currentTarget, isSelected, entity);
        }

        private void SetUnitPathfinding(RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, Vector3 startPos,
            Vector3 endPos, float3 currentTarget, bool isSelected, Entity entity)
        {
            if (NavMesh.CalculatePath(startPos, endPos, _walkableAreaMask, _reusablePath))
            {
                OnPathCalculated(targetPosition, pathComponent, pathBuffer, currentTarget, isSelected, entity);
            }
            else
            {
                OnPathNotAvailable(pathBuffer, targetPosition, pathComponent, currentTarget);
            }
        }

        private void OnPathCalculated(RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent, DynamicBuffer<PathWaypointBuffer> pathBuffer, float3 currentTarget, bool isSelected, Entity entity)
        {
            if (_reusablePath.status == NavMeshPathStatus.PathComplete)
            {
                CompletePath(pathBuffer, pathComponent, currentTarget, isSelected, entity);
            }
            else
            {
                RecalculatePath(pathBuffer, targetPosition, pathComponent, currentTarget);
            }
        }

        private bool GetIsTargetChanged(RefRW<PathComponent> pathComponent, float TARGET_CHANGE_THRESHOLD, float3 currentTarget)
        {
            float3 lastTarget = pathComponent.ValueRO.LastTargetPosition;
            float distanceToLastTarget = math.distance(currentTarget, lastTarget);
            bool targetChanged = distanceToLastTarget > TARGET_CHANGE_THRESHOLD;
            return targetChanged;
        }

        private void OnPathNotAvailable(DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent,
            float3 currentTarget)
        {
            pathBuffer.Clear();
            pathBuffer.Add(new PathWaypointBuffer { Position = targetPosition.ValueRO.Value });
            pathComponent.ValueRW.HasPath = true;
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathComponent.ValueRW.LastTargetPosition = currentTarget;
        }

        private void RecalculatePath(DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRW<UnitTargetPositionComponent> targetPosition, RefRW<PathComponent> pathComponent,
            float3 currentTarget)
        {
            pathBuffer.Clear();
            pathBuffer.Add(new PathWaypointBuffer { Position = targetPosition.ValueRO.Value });
            pathComponent.ValueRW.HasPath = true;
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathComponent.ValueRW.LastTargetPosition = currentTarget;
        }

        private void CompletePath(DynamicBuffer<PathWaypointBuffer> pathBuffer, RefRW<PathComponent> pathComponent, float3 currentTarget, bool isSelected, Entity entity)
        {
            pathBuffer.Clear();

            for (int i = 1; i < _reusablePath.corners.Length; i++)
            {
                pathBuffer.Add(new PathWaypointBuffer { Position = _reusablePath.corners[i] });
            }

            pathComponent.ValueRW.HasPath = pathBuffer.Length > 0;
            pathComponent.ValueRW.CurrentWaypointIndex = 0;
            pathComponent.ValueRW.LastTargetPosition = currentTarget;
        }
    }
}

