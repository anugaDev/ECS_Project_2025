using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

namespace PlayerInputs
{
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
}