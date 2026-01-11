using System.Collections.Generic;
using System.Linq;
using Types;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "UnitsList", menuName = "ScriptableObjects/UnitsList")]
    public class UnitsScriptableObject : ScriptableObject
    {
        [SerializeField]
        private List<UnitScriptableObject> _unitConfigurations;

        public Dictionary<UnitType, UnitScriptableObject> GetUnitsDictionary()
        {
            return _unitConfigurations.ToDictionary(unit => unit.UnitType, unit => unit);
        }
    }
}