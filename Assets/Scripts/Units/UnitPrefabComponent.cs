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
        public Entity Ballista;

        public Entity Warrior;
        
        public Entity Worker;

        public Entity Archer;

    }

    public class UIPrefabs : IComponentData
    {
        public UnitUIController UnitUI;
    }

    public class UITeamColors : IComponentData
    {
        public Color RedColor;

        public Color BlueColor;
    }

    public class UnitsConfigurationComponent : IComponentData
    {
        public UnitsScriptableObject Configuration;
    }
}