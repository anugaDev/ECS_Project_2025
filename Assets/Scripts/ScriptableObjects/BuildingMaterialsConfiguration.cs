using UnityEngine;

namespace Buildings
{
    [CreateAssetMenu(fileName = "BuildingMaterialsConfiguration", menuName = "ScriptableObjects/BuildingMaterialsConfiguration")]
    public class BuildingMaterialsConfiguration : ScriptableObject
    {
        [SerializeField]
        private Material _redTeamMaterial;

        [SerializeField]
        private Material _blueTeamMaterial;
        
        [SerializeField]
        private Material _availableMaterial;

        [SerializeField]
        private Material _notAvailableMaterial;

        public Material RedTeamMaterial => _redTeamMaterial;

        public Material BlueTeamMaterial => _blueTeamMaterial;

        public Material AvailableMaterial => _availableMaterial;

        public Material NotAvailableMaterial => _notAvailableMaterial;
    }
}