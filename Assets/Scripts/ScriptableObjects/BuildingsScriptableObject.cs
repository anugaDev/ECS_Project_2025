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

        public Dictionary<BuildingType, GameObject> GetBuildingsDictionary()
        {
            return _buildingConfigurations.ToDictionary(building => building.BuildingType, building => building.BuildingPrefab);
        }
    }
}