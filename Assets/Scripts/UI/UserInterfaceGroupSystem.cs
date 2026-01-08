using UI.UIControllers;
using Unity.Entities;
using Unity.Transforms;

namespace UI
{
    [UpdateAfter(typeof(TransformSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    public partial class UserInterfaceGroupSystem : SystemBase
    {
        private SelectionActionsDisplayController selectionActionsController;

        protected override void OnCreate()
        {
        }

        protected override void OnUpdate()
        {
        }
    }
}