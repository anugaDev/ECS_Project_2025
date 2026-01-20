using UnityEngine;

namespace Units
{
    [System.Serializable]
    public class UnitTeamMaterials
    {
        [SerializeField]
        private Material _redTeamMaterial;

        [SerializeField]
        private Material _blueTeamMaterial;

        public Material RedTeamMaterial => _redTeamMaterial;
        public Material BlueTeamMaterial => _blueTeamMaterial;
    }
}