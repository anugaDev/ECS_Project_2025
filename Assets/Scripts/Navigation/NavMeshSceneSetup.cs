#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Navigation
{
    /// <summary>
    /// Editor utility to automatically set up NavMesh in the current scene
    /// </summary>
    public class NavMeshSceneSetup : MonoBehaviour
    {
        [MenuItem("Tools/NavMesh/Setup NavMesh in Scene")]
        public static void SetupNavMeshInScene()
        {
            Debug.Log("[NavMeshSceneSetup] Setting up NavMesh in scene...");

            // 1. Create or find NavMesh GameObject
            GameObject navMeshObj = GameObject.Find("NavMesh");
            if (navMeshObj == null)
            {
                navMeshObj = new GameObject("NavMesh");
                Debug.Log("[NavMeshSceneSetup] Created NavMesh GameObject");
            }
            else
            {
                Debug.Log("[NavMeshSceneSetup] Found existing NavMesh GameObject");
            }

            // Add NavMeshSetup component if not present
            NavMeshSetup navMeshSetup = navMeshObj.GetComponent<NavMeshSetup>();
            if (navMeshSetup == null)
            {
                navMeshSetup = navMeshObj.AddComponent<NavMeshSetup>();
                Debug.Log("[NavMeshSceneSetup] Added NavMeshSetup component");
            }

            // IMPORTANT: Manually create NavMeshSurface NOW (don't wait for Awake)
            NavMeshSurface navMeshSurface = navMeshObj.GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = navMeshObj.AddComponent<NavMeshSurface>();

                // Configure it with the correct settings
                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes; // Use RenderMeshes!
                navMeshSurface.layerMask = (1 << 2); // ONLY layer 2 (Ignore Raycast) - excludes building meshes!
                navMeshSurface.agentTypeID = 0;
                navMeshSurface.overrideVoxelSize = true;
                navMeshSurface.voxelSize = 0.1f;

                Debug.Log("[NavMeshSceneSetup] Created NavMeshSurface with RenderMeshes mode (Layer 2 only)");
            }
            else
            {
                // Force correct settings even if it already exists
                navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.layerMask = (1 << 2); // ONLY layer 2
                Debug.Log("[NavMeshSceneSetup] Updated existing NavMeshSurface to RenderMeshes mode (Layer 2 only)");
            }

            // 2. Create or find Ground Plane GameObject
            GameObject groundObj = GameObject.Find("GroundPlane");
            if (groundObj == null)
            {
                groundObj = new GameObject("GroundPlane");
                Debug.Log("[NavMeshSceneSetup] Created GroundPlane GameObject");
            }
            else
            {
                Debug.Log("[NavMeshSceneSetup] Found existing GroundPlane GameObject");
            }

            // Add GroundPlaneSetup component if not present
            GroundPlaneSetup groundSetup = groundObj.GetComponent<GroundPlaneSetup>();
            if (groundSetup == null)
            {
                groundSetup = groundObj.AddComponent<GroundPlaneSetup>();
                Debug.Log("[NavMeshSceneSetup] Added GroundPlaneSetup component");
            }

            // IMPORTANT: Set ground plane to layer 2 (Ignore Raycast)
            groundObj.layer = 2;

            // IMPORTANT: Manually create the ground plane components NOW (don't wait for Awake)
            BoxCollider groundCollider = groundObj.GetComponent<BoxCollider>();
            if (groundCollider == null)
            {
                groundCollider = groundObj.AddComponent<BoxCollider>();
                groundCollider.size = new Vector3(200f, 1f, 200f);
                groundCollider.center = Vector3.zero;
                groundCollider.isTrigger = false;
                groundObj.transform.position = new Vector3(0, -0.5f, 0);
                Debug.Log("[NavMeshSceneSetup] Created BoxCollider on GroundPlane (Layer 2)");
            }

            NavMeshModifier groundModifier = groundObj.GetComponent<NavMeshModifier>();
            if (groundModifier == null)
            {
                groundModifier = groundObj.AddComponent<NavMeshModifier>();
                groundModifier.overrideArea = true;
                groundModifier.area = 0; // Walkable
                Debug.Log("[NavMeshSceneSetup] Created NavMeshModifier on GroundPlane");
            }

            // Mark scene as dirty
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[NavMeshSceneSetup] ✓ Setup complete! Save the scene and press Play to test.");
            Debug.Log("[NavMeshSceneSetup] Objects created:\n" +
                     $"  - {navMeshObj.name} (with NavMeshSetup)\n" +
                     $"  - {groundObj.name} (with GroundPlaneSetup)");
        }

        [MenuItem("Tools/NavMesh/Check Scene Setup")]
        public static void CheckSceneSetup()
        {
            Debug.Log("[NavMeshSceneSetup] Checking scene setup...");

            // Check for NavMesh GameObject
            GameObject navMeshObj = GameObject.Find("NavMesh");
            if (navMeshObj == null)
            {
                Debug.LogError("[NavMeshSceneSetup] ✗ No NavMesh GameObject found!");
            }
            else
            {
                NavMeshSetup setup = navMeshObj.GetComponent<NavMeshSetup>();
                NavMeshSurface surface = navMeshObj.GetComponent<NavMeshSurface>();
                
                Debug.Log($"[NavMeshSceneSetup] ✓ NavMesh GameObject found");
                Debug.Log($"  - NavMeshSetup: {(setup != null ? "✓" : "✗")}");
                Debug.Log($"  - NavMeshSurface: {(surface != null ? "✓" : "✗")}");
                
                if (surface != null)
                {
                    Debug.Log($"  - useGeometry: {surface.useGeometry}");
                    if (surface.useGeometry != NavMeshCollectGeometry.PhysicsColliders)
                    {
                        Debug.LogWarning($"  ⚠ useGeometry should be PhysicsColliders, but is {surface.useGeometry}");
                    }
                }
            }

            // Check for Ground Plane
            GameObject groundObj = GameObject.Find("GroundPlane");
            if (groundObj == null)
            {
                Debug.LogError("[NavMeshSceneSetup] ✗ No GroundPlane GameObject found!");
            }
            else
            {
                GroundPlaneSetup setup = groundObj.GetComponent<GroundPlaneSetup>();
                BoxCollider collider = groundObj.GetComponent<BoxCollider>();
                NavMeshModifier modifier = groundObj.GetComponent<NavMeshModifier>();
                
                Debug.Log($"[NavMeshSceneSetup] ✓ GroundPlane GameObject found");
                Debug.Log($"  - GroundPlaneSetup: {(setup != null ? "✓" : "✗")}");
                Debug.Log($"  - BoxCollider: {(collider != null ? "✓" : "✗")}");
                Debug.Log($"  - NavMeshModifier: {(modifier != null ? "✓" : "✗")}");
            }

            // Check for NavMeshModifierVolumes (obstacles)
            NavMeshModifierVolume[] volumes = GameObject.FindObjectsOfType<NavMeshModifierVolume>();
            Debug.Log($"[NavMeshSceneSetup] Found {volumes.Length} NavMeshModifierVolume(s) in scene");
            foreach (var vol in volumes)
            {
                Debug.Log($"  - {vol.gameObject.name}: size={vol.size}, area={vol.area}");
            }

            Debug.Log("[NavMeshSceneSetup] Check complete!");
        }
    }
}
#endif

