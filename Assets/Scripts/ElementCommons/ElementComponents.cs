using Types;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.UI;

namespace ElementCommons
{
    public struct OwnerTagComponent : IComponentData
    {
    }

    public struct ElementSelectionComponent : IComponentData
    {
        public bool IsSelected;

        public bool MustUpdateUI;
        
        public bool MustUpdateGroup;

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

    public struct TestComponent : IComponentData
    {
    }

    public struct SpawnTestEnemyTeamTag : IComponentData
    {
        public TeamType PlayerTeam;
    }

    public class ElementDisplayDetailsComponent : IComponentData
    {
        public string Name;

        public Sprite Sprite;
    }
}