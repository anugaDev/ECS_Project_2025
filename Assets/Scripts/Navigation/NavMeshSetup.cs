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
        [Tooltip("If true, rebuilds NavMesh on Start(). Set to false to use pre-baked NavMesh from editor.")]
        private bool _buildOnStart = false;

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

        [Header("Layer Settings")]
        [Tooltip("Layers to include in NavMesh baking. Exclude trees, units, and other dynamic objects.")]
        [SerializeField] private LayerMask _includedLayers = ~0; // Default: all layers

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

            // CRITICAL: NavMeshSurface should auto-load, but let's force it
            if (_navMeshSurface != null)
            {
                if (_navMeshSurface.navMeshData != null)
                {
                    Debug.Log($"[NavMeshSetup] NavMeshSurface has data: {_navMeshSurface.navMeshData.name}");
                    Debug.Log($"[NavMeshSetup] Data bounds: {_navMeshSurface.navMeshData.sourceBounds}");

                    // Check if data is actually populated
                    var bounds = _navMeshSurface.navMeshData.sourceBounds;
                    if (bounds.size == Vector3.zero)
                    {
                        Debug.LogError("[NavMeshSetup] ❌ NavMeshData asset is EMPTY! The bake didn't save data to the asset file!");
                        Debug.LogError("  This happens when you bake with SubScene open but the data isn't serialized.");
                        Debug.LogError("  FIX: Delete the NavMesh asset, close SubScene, re-bake, then save scene.");
                    }
                    else
                    {
                        Debug.Log($"[NavMeshSetup] ✓ NavMeshData has valid bounds: {bounds}");

                        // NavMeshSurface should call AddData automatically, but let's verify
                        UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
                        Debug.Log($"[NavMeshSetup] Active NavMesh: Vertices={triangulation.vertices.Length}, Triangles={triangulation.indices.Length / 3}");
                    }
                }
                else
                {
                    Debug.LogError("[NavMeshSetup] ❌ NavMeshSurface has NO navMeshData asset!");
                }
            }
        }

        private void SetupNavMeshSurface()
        {
            _navMeshSurface = GetComponent<NavMeshSurface>();

            if (_navMeshSurface == null)
            {
                _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();

                // Only configure if we just created it
                _navMeshSurface.collectObjects = CollectObjects.Volume;
                _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                _navMeshSurface.size = _navMeshSize;
                _navMeshSurface.center = _navMeshCenter;
                _navMeshSurface.layerMask = _includedLayers;
                _navMeshSurface.agentTypeID = 0;
                _navMeshSurface.overrideVoxelSize = true;
                _navMeshSurface.voxelSize = 0.1f;

                UnityEngine.Debug.Log("[NavMeshSetup] Created new NavMeshSurface with default settings");
            }
            else
            {
                UnityEngine.Debug.Log("[NavMeshSetup] Using existing NavMeshSurface (preserving editor settings)");
            }
        }

        [ContextMenu("Rebuild NavMesh")]
        public void RebuildNavMesh()
        {
            if (_navMeshSurface != null)
            {
                _navMeshSurface.BuildNavMesh();

#if UNITY_EDITOR
                // Force save the NavMesh data asset
                if (_navMeshSurface.navMeshData != null)
                {
                    UnityEditor.EditorUtility.SetDirty(_navMeshSurface.navMeshData);
                    UnityEditor.AssetDatabase.SaveAssets();
                    Debug.Log("[NavMeshSetup] ✓ NavMesh data saved to asset file");
                }
#endif
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

        [ContextMenu("Fix NavMesh Settings")]
        public void FixNavMeshSettings()
        {
            if (_navMeshSurface == null)
            {
                _navMeshSurface = GetComponent<NavMeshSurface>();
            }

            if (_navMeshSurface != null)
            {
                Debug.Log("[NavMeshSetup] Fixing NavMesh settings...");

                // IMPORTANT: Use Volume mode (not All Game Objects) to see SubScene objects
                _navMeshSurface.collectObjects = CollectObjects.Volume;

                // Set correct size for the map
                _navMeshSurface.size = new Vector3(200f, 10f, 200f);
                _navMeshSurface.center = Vector3.zero;

                // Include all layers (especially Default where trees are)
                _navMeshSurface.layerMask = ~0; // All layers

                // Use PhysicsColliders mode
                _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

                Debug.Log($"[NavMeshSetup] ✓ Fixed!");
                Debug.Log($"  CollectObjects: {_navMeshSurface.collectObjects}");
                Debug.Log($"  Size: {_navMeshSurface.size}");
                Debug.Log($"  Center: {_navMeshSurface.center}");
                Debug.Log($"  LayerMask: {_navMeshSurface.layerMask.value}");

                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(_navMeshSurface);
                UnityEditor.EditorUtility.SetDirty(gameObject);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
                UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
                Debug.Log("[NavMeshSetup] ✓ Scene saved! Settings are now persistent.");
                #endif
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

