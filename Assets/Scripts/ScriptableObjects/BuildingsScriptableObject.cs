using System.Collections.Generic;
using System.Linq;
using Types;
using UnityEngine;

namespace ScriptableObjects
{
    [CreateAssetMenu(fileName = "BuildingList", menuName = "ScriptableObjects/BuildingList")]
    public class BuildingsScriptableObject : ScriptableObject
    {
        [SerializeField]
        private List<BuildingScriptableObject> _buildings;

        public Dictionary<BuildingType, GameObject> GetBuildingsDictionary()
        {
            return _buildings.ToDictionary(building => building.BuildingType, building => building.BuildingPrefab);
        }
    }
}