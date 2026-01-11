using System;
using Types;
using UnityEngine;

namespace ScriptableObjects
{
    [Serializable]
    public class UnitScriptableObject
    {
        [SerializeField]
        private UnitType _unitType;
        
        [SerializeField]
        private GameObject _unitPrefab;

        [SerializeField]
        private string _name;

        [SerializeField]
        private string _description;

        public UnitType UnitType => _unitType;

        public GameObject UnitPrefab => _unitPrefab;

        public string Name => _name;

        public string Description => _description;
    }
}