using System.Collections.Generic;
using System.Linq;
using Types;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "BuildingsList", menuName = "ScriptableObjects/BuildingsList")]
    public class BuildingsScriptableObject : ScriptableObject
    {
        [SerializeField]
        private List<BuildingScriptableObject> _buildingConfigurations;
        
        [Header("Tower Combat Properties")]
        [SerializeField]
        private float _towerAttackRange;

        [SerializeField]
        private int _towerDamagePerSecond;

        public float TowerAttackRange => _towerAttackRange;

        public int TowerDamagePerSecond => _towerDamagePerSecond;

        public Dictionary<BuildingType, BuildingScriptableObject> GetBuildingsDictionary()
        {
            return _buildingConfigurations.ToDictionary(building => building.BuildingType, building => building);
        }
    }
}