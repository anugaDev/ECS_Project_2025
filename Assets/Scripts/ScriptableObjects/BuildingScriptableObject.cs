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
        private GameObject _buildingPrefab;

        public BuildingType BuildingType => _buildingType;

        public GameObject BuildingPrefab => _buildingPrefab;
    }
}