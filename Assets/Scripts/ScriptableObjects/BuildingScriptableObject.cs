using System;
using Types;
using UnityEngine;

namespace ScriptableObjects
{
    [Serializable]
    public class BuildingScriptableObject
    {
        [SerializeField]
        private BuildingType _buildingType;
        
        [SerializeField]
        private string _name;

        [SerializeField]
        private string _description;
        
        [SerializeField]
        private GameObject _buildingPrefab;

        public BuildingType BuildingType => _buildingType;

        public GameObject BuildingPrefab => _buildingPrefab;

        public string Name => _name;

        public string Description => _description;
    }
}