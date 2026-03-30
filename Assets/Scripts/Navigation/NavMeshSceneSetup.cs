#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Navigation
{
    public class NavMeshSceneSetup : MonoBehaviour
    {
        [MenuItem("Tools/NavMesh/Setup NavMesh in Scene")]
        public static void SetupNavMeshInScene()
        {
            Debug.Log("[NavMeshSceneSetup] Setting up NavMesh in scene...");

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

            NavMeshSetup navMeshSetup = navMeshObj.GetComponent<NavMeshSetup>();
            if (navMeshSetup == null)
            {
                navMeshSetup = navMeshObj.AddComponent<NavMeshSetup>();
                Debug.Log("[NavMeshSceneSetup] Added NavMeshSetup component");
            }

            NavMeshSurface navMeshSurface = navMeshObj.GetComponent<NavMeshSurface>();
            if (navMeshSurface == null)
            {
                navMeshSurface = navMeshObj.AddComponent<NavMeshSurface>();

                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;                navMeshSurface.layerMask = (1 << 2);                navMeshSurface.agentTypeID = 0;
                navMeshSurface.overrideVoxelSize = true;
                navMeshSurface.voxelSize = 0.1f;

                Debug.Log("[NavMeshSceneSetup] Created NavMeshSurface with RenderMeshes mode (Layer 2 only)");
            }
            else
            {
                navMeshSurface.useGeometry = NavMeshCollectGeometry.RenderMeshes;
                navMeshSurface.collectObjects = CollectObjects.All;
                navMeshSurface.layerMask = (1 << 2);                Debug.Log("[NavMeshSceneSetup] Updated existing NavMeshSurface to RenderMeshes mode (Layer 2 only)");
            }

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

            GroundPlaneSetup groundSetup = groundObj.GetComponent<GroundPlaneSetup>();
            if (groundSetup == null)
            {
                groundSetup = groundObj.AddComponent<GroundPlaneSetup>();
                Debug.Log("[NavMeshSceneSetup] Added GroundPlaneSetup component");
            }

            groundObj.layer = 2;

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
                groundModifier.area = 0;                Debug.Log("[NavMeshSceneSetup] Created NavMeshModifier on GroundPlane");
            }

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

