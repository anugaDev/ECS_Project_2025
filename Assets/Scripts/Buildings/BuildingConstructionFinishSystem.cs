using Unity.Entities;

namespace Buildings
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateAfter(typeof(InitializeBuildingSystem))]
    public partial class BuildingConstructionFinishSystem : SystemBase
    {
        protected override void OnCreate()
        {
            RequireForUpdate<BuildingConstructionProgressComponent>();
        }

        protected override void OnUpdate()
        {
            foreach ((BuildingConstructionProgressComponent constructionProgress,
                      BuildingPivotReferencesComponent pivotReferences,
                      Entity buildingEntity)
                     in SystemAPI.Query<BuildingConstructionProgressComponent,
                                        BuildingPivotReferencesComponent>()
                         .WithAll<BuildingComponents>()
                         .WithEntityAccess())
            {
                if (!constructionProgress.IsFinished)
                    continue;

                if (pivotReferences.Pivot != null)
                    pivotReferences.Pivot.SetActive(true);

                if (pivotReferences.ConstructionSiteObject != null)
                    pivotReferences.ConstructionSiteObject.SetActive(false);
            }
        }
    }
}
