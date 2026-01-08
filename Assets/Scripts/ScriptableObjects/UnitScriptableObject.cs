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

        public UnitType UnitType => _unitType;

        public GameObject UnitPrefab => _unitPrefab;
    }
}