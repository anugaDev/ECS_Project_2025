using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public class UnitMaterialTargetAuthoring : MonoBehaviour
    {
        [SerializeField, Tooltip("The renderers that should receive team colors")]
        private List<Renderer> _targetRenderers = new List<Renderer>();

        public List<Renderer> TargetRenderers => _targetRenderers;

        public class UnitMaterialTargetBaker : Baker<UnitMaterialTargetAuthoring>
        {
            public override void Bake(UnitMaterialTargetAuthoring authoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.Dynamic);

                if (authoring.TargetRenderers != null && authoring.TargetRenderers.Count > 0)
                {
                    AddComponentObject(entity, new UnitMaterialTargetComponent
                    {
                        TargetRenderers = new List<Renderer>(authoring.TargetRenderers)
                    });
                }
            }
        }
    }

    public class UnitMaterialTargetComponent : IComponentData
    {
        public List<Renderer> TargetRenderers;
    }
}

