using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace Navigation
{
    public class NavMeshSetup : MonoBehaviour
    {
        [Header("NavMesh Settings")]
        [SerializeField] 
        private Vector3 _navMeshSize;
        
        [SerializeField] 
        private Vector3 _navMeshCenter = Vector3.zero;
        
        [SerializeField] 
        private bool _buildOnStart = true;

        [Header("Agent Settings")]
        [SerializeField]
        private float _agentRadius = 0.5f;
        
        [SerializeField] 
        private float _agentHeight = 2f;
        
        [SerializeField] 
        private float _maxSlope = 45f;
        
        [SerializeField] 
        private float _stepHeight = 0.4f;

        [Header("Visualization")]
        [SerializeField] private bool _showGizmos = true;

        private NavMeshSurface _navMeshSurface;

        private void Awake()
        {
            SetupNavMeshSurface();
        }

        private void Start()
        {
            if (_buildOnStart && _navMeshSurface != null)
            {
                _navMeshSurface.BuildNavMesh();
            }
        }

        private void SetupNavMeshSurface()
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();

            if (_navMeshSurface == null)
            {
                _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            }

            _navMeshSurface.collectObjects = CollectObjects.Volume;
            _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            _navMeshSurface.size = _navMeshSize;
            _navMeshSurface.center = _navMeshCenter;
            _navMeshSurface.layerMask = ~0;
            _navMeshSurface.agentTypeID = 0;
            _navMeshSurface.overrideVoxelSize = true;
            _navMeshSurface.voxelSize = 0.1f;
        }

        [ContextMenu("Rebuild NavMesh")]
        public void RebuildNavMesh()
        {
            if (_navMeshSurface != null)
            {
                _navMeshSurface.BuildNavMesh();
            }
        }

        [ContextMenu("Check NavMesh Status")]
        public void CheckNavMeshStatus()
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 100f, NavMesh.AllAreas))
            {
                NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_showGizmos) return;

            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireCube(_navMeshCenter, _navMeshSize);

            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2, "NavMesh Volume");
            #endif
        }
    }
}

