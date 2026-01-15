using Types;
using Unity.Entities;
using Unity.NetCode;

namespace ElementCommons
{
    public struct OwnerTagComponent : IComponentData
    {
    }

    public struct ElementSelectionComponent : IComponentData
    {
        public bool IsSelected;

        public bool MustUpdateUI;

        public bool MustEnableFeedback;
    }

    public struct SelectableElementTypeComponent : IComponentData
    {
        [GhostField] 
        public SelectableElementType Type;
    }

    public struct ElementTeamComponent : IComponentData
    {
        [GhostField] 
        public TeamType Team;
    }
}