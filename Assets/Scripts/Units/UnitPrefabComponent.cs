using UI;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public struct UnitPrefabComponent : IComponentData
    {
        public Entity Unit;
    }

    public class UIPrefabs : IComponentData
    {
        public HealthBarView HealthBar;
    }
}