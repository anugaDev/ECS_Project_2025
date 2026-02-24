using ElementCommons;
using Types;
using Unity.Collections;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine.Rendering;

namespace Buildings
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class InitializeBuildingSystem : SystemBase
    {
        private BuildingMaterialsConfiguration _materialsConfiguration;
        
        private BatchMaterialID _redMaterial;

        private BatchMaterialID _blueMaterial;

        protected override void OnCreate()
        {
            RequireForUpdate<BuildingMaterialsConfigurationComponent>();
        }

        protected override void OnStartRunning()
        {
            _materialsConfiguration = SystemAPI.ManagedAPI.GetSingleton<BuildingMaterialsConfigurationComponent>().Configuration;
            EntitiesGraphicsSystem entitiesGraphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            _redMaterial = entitiesGraphicsSystem.RegisterMaterial(_materialsConfiguration.RedTeamMaterial);
            _blueMaterial = entitiesGraphicsSystem.RegisterMaterial(_materialsConfiguration.BlueTeamMaterial);
        }

        protected override void OnUpdate()
        {
            EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

            foreach ((ElementTeamComponent buildingTeam, DynamicBuffer<LinkedEntityGroup> linkedEntities, Entity buildingEntity)
                     in SystemAPI.Query<ElementTeamComponent, DynamicBuffer<LinkedEntityGroup>>()
                         .WithAll<NewBuildingTagComponent>().WithEntityAccess())
            {
                SetTeamMaterialOnChildren(linkedEntities, buildingTeam.Team, entityCommandBuffer);
            }

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();

            // Update construction visuals for all buildings
            UpdateConstructionVisuals();
        }

        private void UpdateConstructionVisuals()
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

            foreach ((BuildingPivotReferencesComponent pivotRefs,
                      BuildingTypeComponent _,
                      Entity buildingEntity)
                     in SystemAPI.Query<BuildingPivotReferencesComponent,
                                        BuildingTypeComponent>()
                         .WithEntityAccess())
            {
                bool isFinished = true;

                if (EntityManager.HasComponent<BuildingConstructionProgressComponent>(buildingEntity))
                {
                    BuildingConstructionProgressComponent progress =
                        EntityManager.GetComponentData<BuildingConstructionProgressComponent>(buildingEntity);

                    isFinished = progress.ConstructionTime > 0 && progress.Value >= progress.ConstructionTime;
                }

                if (!EntityManager.HasBuffer<LinkedEntityGroup>(buildingEntity))
                    continue;

                DynamicBuffer<LinkedEntityGroup> linkedEntities = 
                    EntityManager.GetBuffer<LinkedEntityGroup>(buildingEntity);

                for (int i = 0; i < linkedEntities.Length; i++)
                {
                    Entity childEntity = linkedEntities[i].Value;
                    if (childEntity == buildingEntity)
                        continue;

                    bool belongsToPivot = IsChildOf(childEntity, pivotRefs.PivotEntity);
                    bool belongsToSite = IsChildOf(childEntity, pivotRefs.ConstructionSiteEntity);

                    if (belongsToPivot)
                        SetEntityEnabled(childEntity, isFinished, ecb);
                    else if (belongsToSite)
                        SetEntityEnabled(childEntity, !isFinished, ecb);
                }
            }

            ecb.Playback(EntityManager);
            ecb.Dispose();
        }

        private bool IsChildOf(Entity entity, Entity parent)
        {
            if (parent == Entity.Null)
                return false;
            if (entity == parent)
                return true;

            Entity current = entity;
            while (EntityManager.HasComponent<Unity.Transforms.Parent>(current))
            {
                Entity parentEntity = EntityManager.GetComponentData<Unity.Transforms.Parent>(current).Value;
                if (parentEntity == parent)
                    return true;
                current = parentEntity;
            }

            return false;
        }

        private void SetEntityEnabled(Entity entity, bool enabled, EntityCommandBuffer ecb)
        {
            if (!EntityManager.Exists(entity))
                return;

            if (enabled)
            {
                if (EntityManager.HasComponent<Disabled>(entity))
                    ecb.RemoveComponent<Disabled>(entity);
            }
            else
            {
                if (!EntityManager.HasComponent<Disabled>(entity))
                    ecb.AddComponent<Disabled>(entity);
            }
        }

        private void SetTeamMaterialOnChildren(DynamicBuffer<LinkedEntityGroup> linkedEntities, TeamType team, EntityCommandBuffer ecb)
        {
            BatchMaterialID batchMaterialID = team == TeamType.Red ? _redMaterial : _blueMaterial;

            for (int i = 0; i < linkedEntities.Length; i++)
            {
                Entity childEntity = linkedEntities[i].Value;

                if (EntityManager.HasComponent<MaterialMeshInfo>(childEntity))
                {
                    MaterialMeshInfo materialMeshInfo = EntityManager.GetComponentData<MaterialMeshInfo>(childEntity);
                    materialMeshInfo.MaterialID = batchMaterialID;
                    ecb.SetComponent(childEntity, materialMeshInfo);
                }
            }
        }
    }
}