using ElementCommons;
using PlayerInputs;
using Types;
using Unity.Entities;
using Unity.Rendering;
using Units.Worker;
using UnityEngine;

namespace Units
{
    public class UnitAuthoring : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField]
        private float _moveSpeed = 5f;

        [Header("Unit Properties")]
        [SerializeField]
        private UnitType _unitType;

        [SerializeField]
        private UnitTeamMaterials _teamMaterials;

        public float MoveSpeed => _moveSpeed;

        public UnitType UnitType => _unitType;

        public UnitTeamMaterials TeamMaterials => _teamMaterials;

        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PathComponent>(unitEntity);
                AddComponent<UnitStateComponent>(unitEntity);
                AddComponent<UnitTagComponent>(unitEntity);
                AddComponent<NewUnitTagComponent>(unitEntity);
                AddComponent<ElementTeamComponent>(unitEntity);
                AddComponent<URPMaterialPropertyBaseColor>(unitEntity);
                AddComponent<SetInputStateTargetComponent>(unitEntity);
                AddComponent<SetServerStateTargetComponent>(unitEntity);

                AddComponent<ElementSelectionComponent>(unitEntity);
                AddComponent<ElementDisplayDetailsComponent>(unitEntity);
                AddComponent(unitEntity, GetUnitTypeComponent(authoring));
                AddComponent(unitEntity, GetMoveSpeedComponent(authoring));
                AddComponent(unitEntity, GetSelectableTypeComponent());
                AddComponentObject(unitEntity, GetUnitMaterialsComponent(authoring));
                AddBuffer<PathWaypointBuffer>(unitEntity);
                AddComponent<UnitWaypointsInputComponent>(unitEntity);
            }

            private SelectableElementTypeComponent GetSelectableTypeComponent()
            {
                return new SelectableElementTypeComponent
                {
                    Type = SelectableElementType.Unit
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

            private UnitMaterialsComponent GetUnitMaterialsComponent(UnitAuthoring authoring)
            {
                return new UnitMaterialsComponent
                {
                    RedTeamMaterial = authoring.TeamMaterials?.RedTeamMaterial,
                    BlueTeamMaterial = authoring.TeamMaterials?.BlueTeamMaterial
                };
            }
        }
    }
}