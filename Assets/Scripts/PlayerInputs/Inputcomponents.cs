using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace PlayerInputs
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct UnitTargetPositionComponent : IInputComponentData
    {
        [GhostField(Quantization = 0)] 
        public float3 Value;
        
        [GhostField(Quantization = 0)] 
        public bool MustMove;
    }
    public struct NewSelectionComponent : IInputComponentData
    {
        [GhostField(Quantization = 0)] 
        public Rect SelectionRect;
        
        [GhostField(Quantization = 0)]
        public bool MustKeepSelection;

        [GhostField(Quantization = 0)]
        public bool IsClickSelection;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct KeyShortcutInputComponent : IInputComponentData
    {
        [GhostField]
        public InputEvent SelectBaseInput;
    }

    /// <summary>
    /// Component to track a selected target entity that the unit should follow
    /// </summary>
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct UnitSelectedTargetComponent : IInputComponentData
    {
        /// <summary>
        /// The entity that this unit is targeting (can be enemy unit, resource, or building)
        /// </summary>
        [GhostField]
        public Entity TargetEntity;

        /// <summary>
        /// Whether this unit is currently following a target (vs just moving to a position)
        /// </summary>
        [GhostField]
        public bool IsFollowingTarget;
    }
}