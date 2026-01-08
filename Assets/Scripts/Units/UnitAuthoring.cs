using PlayerInputs;
using Types;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;

namespace Units
{
    public class UnitAuthoring : MonoBehaviour
    {
        [SerializeField]
        private float _moveSpeed;
        
        [SerializeField]
        private UnitType _unitType;

        public float MoveSpeed => _moveSpeed;

        public UnitType UnitType => _unitType;

        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UnitTagComponent>(unitEntity);
                AddComponent<NewUnitTagComponent>(unitEntity);
                AddComponent<EntityTeamComponent>(unitEntity);
                AddComponent<URPMaterialPropertyBaseColor>(unitEntity);
                AddComponent<UnitTargetPositionComponent>(unitEntity);
                AddComponent<UnitSelectionComponent>(unitEntity);
                AddComponent(unitEntity, GetMoveSpeedComponent(authoring));
                AddComponent(unitEntity, GetUnitTypeComponent(authoring));
            }

            private UnitMoveSpeedComponent GetMoveSpeedComponent(UnitAuthoring authoring)
            {
                return new UnitMoveSpeedComponent
                {
                    Speed = authoring.MoveSpeed
                };
            }
            private UnitTypeComponent GetUnitTypeComponent(UnitAuthoring authoring)
            {
                return new UnitTypeComponent
                {
                    Type = authoring.UnitType
                };
            }
        }
    }
}