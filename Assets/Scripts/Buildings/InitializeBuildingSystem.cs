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

                if (EntityManager.HasComponent<BuildingPivotReferencesComponent>(buildingEntity))
                {
                    BuildingPivotReferencesComponent pivotReferences = EntityManager.GetComponentObject<BuildingPivotReferencesComponent>(buildingEntity);
                    if (pivotReferences.ConstructionSiteObject != null)
                        pivotReferences.ConstructionSiteObject.SetActive(true);
                    if (pivotReferences.Pivot != null)
                        pivotReferences.Pivot.SetActive(false);
                }
            }

            entityCommandBuffer.Playback(EntityManager);
            entityCommandBuffer.Dispose();
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