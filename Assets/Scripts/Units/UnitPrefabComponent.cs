using System.Collections.Generic;
using ScriptableObjects;
using Types;
using UI;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public struct UnitPrefabComponent : IComponentData
    {
        public Entity Worker;
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