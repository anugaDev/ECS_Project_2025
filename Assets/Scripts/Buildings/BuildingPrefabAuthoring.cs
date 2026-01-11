using ScriptableObjects;
using Unity.Entities;
using UnityEngine;

namespace Buildings
{
    public class BuildingPrefabAuthoring : MonoBehaviour
    {
        [SerializeField]
        private BuildingsScriptableObject _configuration;

        public BuildingsScriptableObject Configuration => _configuration;

        public class BuildingPrefabBaker : Baker<BuildingPrefabAuthoring>
        {
            public override void Bake(BuildingPrefabAuthoring prefabAuthoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(prefabContainerEntity, GetBuildingConfiguration(prefabAuthoring));
            }

            private BuildingsConfigurationComponent GetBuildingConfiguration(BuildingPrefabAuthoring prefabAuthoring)
            {
                return new BuildingsConfigurationComponent
                {
                    Configuration =  prefabAuthoring.Configuration
                };
            }
        }
    }
}