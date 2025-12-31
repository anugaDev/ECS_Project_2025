using Client;
using PlayerInputs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

namespace Units
{
    public class UnitAuthoring : MonoBehaviour
    {
        [SerializeField]
        private float MoveSpeed;

        public class UnitBaker : Baker<UnitAuthoring>
        {
            public override void Bake(UnitAuthoring authoring)
            {
                Entity unitEntity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<UnitTagComponent>(unitEntity);
                AddComponent<NewUnitTagComponent>(unitEntity);
                AddComponent<UnitTeamComponent>(unitEntity);
                AddComponent<URPMaterialPropertyBaseColor>(unitEntity);
                AddComponent<UnitTargetPositionComponent>(unitEntity);
                AddComponent<UnitSelectionComponent>(unitEntity);
                AddComponent(unitEntity, GetMoveSpeedComponent(authoring));
            }

            private UnitMoveSpeedComponent GetMoveSpeedComponent(UnitAuthoring authoring)
            {
                return new UnitMoveSpeedComponent
                {
                    Speed = authoring.MoveSpeed
                };
            }
        }
    }
}