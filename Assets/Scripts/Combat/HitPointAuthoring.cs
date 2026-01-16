using UI;
using Unity.Entities;
using UnityEngine;

namespace Combat
{
    public class HitPointAuthoring : MonoBehaviour
    {
        [SerializeField]
        private int MaxHitPoints;
        
        [SerializeField]
        public Vector3 HealthBarOffset;

        public class HitPointsBaker: Baker<HitPointAuthoring>
        {
            public override void Bake(HitPointAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, GetHitPointsComponent(authoring));
                AddComponent(entity, GetMaxHitPointsComponent(authoring));
                AddBuffer<DamageBufferElement>(entity);
                AddBuffer<CurrentTickDamageCommand>(entity);
                AddComponent(entity, GetHealthBarOffsetComponent(authoring));
            }

            private SelectionFeedbackOffset GetHealthBarOffsetComponent(HitPointAuthoring authoring)
            {
                return new SelectionFeedbackOffset { HealthBarOffset = authoring.HealthBarOffset };
            }

            private MaxHitPointsComponent GetMaxHitPointsComponent(HitPointAuthoring authoring)
            {
                return new MaxHitPointsComponent
                {
                    Value = authoring.MaxHitPoints
                };
            }

            private CurrentHitPointsComponent GetHitPointsComponent(HitPointAuthoring authoring)
            {
                return new CurrentHitPointsComponent
                {
                    Value = authoring.MaxHitPoints
                };
            }
        }
    }
}