using Unity.Entities;
using UnityEngine;

namespace PlayerInputs.MoveIndicator
{
    public class MoveIndicatorPrefabAuthoring : MonoBehaviour
    {
        [SerializeField] 
        private MoveIndicatorController _moveIndicator;

        public MoveIndicatorController MoveIndicator => _moveIndicator;

        public class MoveIndicatorPrefabBaker : Baker<MoveIndicatorPrefabAuthoring>
        {
            public override void Bake(MoveIndicatorPrefabAuthoring prefabAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, GetMoveIndicatorPrefabComponent(prefabAuthoring));
            }

            private MoveIndicatorPrefabComponent GetMoveIndicatorPrefabComponent(MoveIndicatorPrefabAuthoring prefabAuthoring)
            {
                return new MoveIndicatorPrefabComponent
                {
                    Value = prefabAuthoring.MoveIndicator
                };
            }
        }
    }
}