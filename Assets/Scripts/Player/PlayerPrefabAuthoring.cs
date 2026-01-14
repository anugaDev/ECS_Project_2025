using Buildings;
using Unity.Entities;
using UnityEngine;

namespace UI
{
    public class PlayerPrefabAuthoring : MonoBehaviour
    {
        [SerializeField]
        private GameObject _playerPrefab;
        
        public GameObject PlayerPrefab => _playerPrefab;
        public class PlayerBaker : Baker<PlayerPrefabAuthoring>
        {
            public override void Bake(PlayerPrefabAuthoring playerAuthoring)
            {
                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponent(entity, new PlayerPrefabComponent
                {
                    Entity = GetEntity(playerAuthoring.PlayerPrefab, TransformUsageFlags.Dynamic),
                });
            }
        }
    }
}