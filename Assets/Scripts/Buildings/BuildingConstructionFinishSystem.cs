using Unity.Entities;

namespace Buildings
{
    // Visual swap logic is handled in InitializeBuildingSystem.UpdateConstructionVisuals()
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateAfter(typeof(InitializeBuildingSystem))]
    public partial class BuildingConstructionFinishSystem : SystemBase
    {
        protected override void OnUpdate()
        {
        }
    }
}
