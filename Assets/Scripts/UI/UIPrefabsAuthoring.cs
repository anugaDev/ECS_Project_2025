using Unity.Entities;
using UnityEngine;

namespace UI
{
    public class UIPrefabsAuthoring : MonoBehaviour
    { 
        [SerializeField] 
        private UnitUIController _unitUIPrefab;

        [SerializeField] 
        private GameObject _resourceUIPrefab;
        
        public UnitUIController UnitUIPrefab => _unitUIPrefab;
        
        public GameObject ResourceUIPrefab => _resourceUIPrefab;

        public class UnitPrefabBaker : Baker<UIPrefabsAuthoring>
        {
            public override void Bake(UIPrefabsAuthoring prefabAuthoring)
            {
                Entity prefabContainerEntity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(prefabContainerEntity, GetUIPrefabs(prefabAuthoring));
            }

            private UIPrefabs GetUIPrefabs(UIPrefabsAuthoring prefabAuthoring)
            {
                return new UIPrefabs
                {
                    UnitUI = prefabAuthoring.UnitUIPrefab,
                    ResourceUI = prefabAuthoring.ResourceUIPrefab
                };
            }
        }
    }
}