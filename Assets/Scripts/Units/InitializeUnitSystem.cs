using ElementCommons;
using Types;
using Units;
using Units.Worker;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine.Rendering;
using Material = UnityEngine.Material;

namespace Server
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class InitializeUnitSystem : SystemBase
    {
        private EntityCommandBuffer _entityCommandBuffer;

        protected override void OnUpdate()
        {
            _entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);
            EntitiesGraphicsSystem entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();

            foreach ((RefRO<LocalTransform> transform,
                     RefRW<SetInputStateTargetComponent> inputTarget,
                     ElementTeamComponent unitTeam,
                     DynamicBuffer<LinkedEntityGroup> linkedEntities,
                     Entity unitEntity)
                     in SystemAPI.Query<RefRO<LocalTransform>,
                                       RefRW<SetInputStateTargetComponent>,
                                       ElementTeamComponent,
                                       DynamicBuffer<LinkedEntityGroup>>()
                         .WithAll<NewUnitTagComponent>()
                         .WithEntityAccess())
            {
                InitializeInputTarget(inputTarget, transform);
                SetTeamMaterialOnMarkedChildren(linkedEntities, unitTeam.Team, unitEntity, entitiesGraphicsSystem);
                _entityCommandBuffer.RemoveComponent<NewUnitTagComponent>(unitEntity);
            }

            _entityCommandBuffer.Playback(EntityManager);
            _entityCommandBuffer.Dispose();
        }

        private void SetTeamMaterialOnMarkedChildren(DynamicBuffer<LinkedEntityGroup> linkedEntities,
            TeamType team, Entity unitEntity, EntitiesGraphicsSystem entitiesGraphicsSystem)
        {
            if (!EntityManager.HasComponent<UnitMaterialsComponent>(unitEntity))
            {
                return;
            }

            UnitMaterialsComponent materialsComponent = EntityManager.GetComponentData<UnitMaterialsComponent>(unitEntity);
            Material teamMaterial = team == TeamType.Red ? materialsComponent.RedTeamMaterial : materialsComponent.BlueTeamMaterial;

            if (teamMaterial == null)
            {
                return;
            }

            BatchMaterialID batchMaterialID = entitiesGraphicsSystem.RegisterMaterial(teamMaterial);

            for (int i = 0; i < linkedEntities.Length; i++)
            {
                SetMaterialComponent(linkedEntities, i, batchMaterialID);
            }
        }

        private void SetMaterialComponent(DynamicBuffer<LinkedEntityGroup> linkedEntities, int i, BatchMaterialID batchMaterialID)
        {
            Entity childEntity = linkedEntities[i].Value;
            if (!EntityManager.HasComponent<MaterialMeshInfo>(childEntity))
            {
                return;
            }

            MaterialMeshInfo materialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(childEntity);
            materialMeshInfo.MaterialID = batchMaterialID;
            _entityCommandBuffer.SetComponent(childEntity, materialMeshInfo);
        }

        private void InitializeInputTarget(RefRW<SetInputStateTargetComponent> inputTarget, RefRO<LocalTransform> transform)
        {
            // Initialize target to unit's spawn position so it doesn't move on spawn
            inputTarget.ValueRW.TargetPosition = transform.ValueRO.Position;
            inputTarget.ValueRW.TargetEntity = Entity.Null;
            inputTarget.ValueRW.IsFollowingTarget = false;
            inputTarget.ValueRW.StoppingDistance = 0f;
            inputTarget.ValueRW.HasNewTarget = false; // No new target on spawn
        }


    }
}