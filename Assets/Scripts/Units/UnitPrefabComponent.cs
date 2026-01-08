using ScriptableObjects;
using UI;
using Unity.Entities;

namespace Units
{
    public struct UnitPrefabComponent : IComponentData
    {
        public Entity Unit;
    }

    public class UIPrefabs : IComponentData
    {
        public UnitUIController UnitUI;
    }

    public class UnitsConfigurationComponent : IComponentData
    {
        public UnitsScriptableObject Configuration;
    }
}