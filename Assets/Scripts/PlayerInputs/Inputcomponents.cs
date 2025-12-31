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
    public struct SelectionBoxPositionComponent : IInputComponentData
    {
        [GhostField(Quantization = 0)] 
        public Rect Value;
    }

    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct KeyShortcutInputComponent : IInputComponentData
    {
        [GhostField]
        public InputEvent SelectBaseInput;
    }
}