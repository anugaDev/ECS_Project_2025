using ElementCommons;
using PlayerInputs;
using Types;
using Unity.Entities;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Serialization;

namespace Units
{
    public class UnitAuthoring : MonoBehaviour
    {
        [SerializeField]
        private float _moveSpeed;
        
        [SerializeField]
        private UnitType _unitType;

        [SerializeField]
        private SelectableElementType _selectableType;

        public float MoveSpeed => _moveSpeed;

        public UnitType UnitType => _unitType;

        public SelectableElementType SelectableType => _selectableType;

        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UnitTagComponent>(unitEntity);
                AddComponent<NewUnitTagComponent>(unitEntity);
                AddComponent<ElementTeamComponent>(unitEntity);
                AddComponent<URPMaterialPropertyBaseColor>(unitEntity);
                AddComponent<UnitTargetPositionComponent>(unitEntity);
                AddComponent<ElementSelectionComponent>(unitEntity);
                AddComponent<ElementDisplayDetailsComponent>(unitEntity);
                AddComponent(unitEntity, GetUnitTypeComponent(authoring));
                AddComponent(unitEntity, GetMoveSpeedComponent(authoring));
                AddComponent(unitEntity, GetSelectableTypeComponent(authoring));
            }

            private SelectableElementTypeComponent GetSelectableTypeComponent(UnitAuthoring authoring)
            {
                return new SelectableElementTypeComponent
                {
                    Type = authoring.SelectableType
                };
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