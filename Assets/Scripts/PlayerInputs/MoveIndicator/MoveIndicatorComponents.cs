using Unity.Entities;

namespace PlayerInputs.MoveIndicator
{
    public class MoveIndicatorPrefabComponent : IComponentData
    {
        public MoveIndicatorController Value;
    }
}