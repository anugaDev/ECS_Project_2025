#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using Unity.Scenes;

public class BakeNavMeshWithSubScene : EditorWindow
{
    [MenuItem("Tools/NavMesh/Bake NavMesh with SubScene")]
    public static void BakeWithSubScene()
    {
        Debug.Log("[BakeNavMesh] Starting NavMesh bake with SubScene...");
        
        // 1. Find all SubScenes
        SubScene[] subScenes = GameObject.FindObjectsOfType<SubScene>();
        
        if (subScenes.Length == 0)
        {
            Debug.LogWarning("[BakeNavMesh] No SubScenes found. Baking without SubScene...");
        }
        else
        {
            Debug.Log($"[BakeNavMesh] Found {subScenes.Length} SubScene(s)");
            
            // Make sure SubScenes are loaded
            foreach (SubScene subScene in subScenes)
            {
                if (!subScene.IsLoaded)
                {
                    Debug.LogWarning($"[BakeNavMesh] SubScene '{subScene.name}' is NOT loaded!");
                    Debug.LogWarning($"  → Right-click SubScene in Hierarchy → 'Open Scene Additive'");
                    EditorUtility.DisplayDialog("SubScene Not Loaded", 
                        $"SubScene '{subScene.name}' must be loaded!\n\n" +
                        "Right-click the SubScene in Hierarchy and select 'Open Scene Additive'", 
                        "OK");
                    return;
                }
                else
                {
                    Debug.Log($"[BakeNavMesh] ✓ SubScene '{subScene.name}' is loaded");
                }
            }
        }
        
        // 2. Find NavMeshSurface
        NavMeshSurface navMeshSurface = GameObject.FindObjectOfType<NavMeshSurface>();
        
        if (navMeshSurface == null)
        {
            Debug.LogError("[BakeNavMesh] No NavMeshSurface found in scene!");
            EditorUtility.DisplayDialog("Error", "No NavMeshSurface found!\n\nAdd a NavMeshSurface component to a GameObject.", "OK");
            return;
        }
        
        Debug.Log($"[BakeNavMesh] Found NavMeshSurface on: {navMeshSurface.gameObject.name}");
        Debug.Log($"  CollectObjects: {navMeshSurface.collectObjects}");
        Debug.Log($"  UseGeometry: {navMeshSurface.useGeometry}");
        Debug.Log($"  LayerMask: {navMeshSurface.layerMask.value}");
        Debug.Log($"  Size: {navMeshSurface.size}");
        
        // 3. Count colliders that will be included
        Collider[] allColliders = GameObject.FindObjectsOfType<Collider>();
        int includedColliders = 0;
        int treeColliders = 0;
        
        foreach (Collider col in allColliders)
        {
            // Check if collider's layer is included in the layer mask
            if ((navMeshSurface.layerMask.value & (1 << col.gameObject.layer)) != 0)
            {
                includedColliders++;
                
                if (col.gameObject.name.Contains("Tree") || col.gameObject.name.Contains("tree"))
                {
                    treeColliders++;
                }
            }
        }
        
        Debug.Log($"[BakeNavMesh] Total colliders in scene: {allColliders.Length}");
        Debug.Log($"[BakeNavMesh] Colliders included in NavMesh: {includedColliders}");
        Debug.Log($"[BakeNavMesh] Tree colliders: {treeColliders}");
        
        if (treeColliders == 0)
        {
            Debug.LogWarning("[BakeNavMesh] ⚠ No tree colliders found!");
            Debug.LogWarning("  Make sure trees have Unity Colliders (not just ECS Physics Shapes)");
            Debug.LogWarning("  Run: Tools → NavMesh → Add Colliders to Trees");
        }
        
        // 4. Bake NavMesh
        Debug.Log("[BakeNavMesh] Baking NavMesh...");
        navMeshSurface.BuildNavMesh();
        
        // 5. Verify result
        UnityEngine.AI.NavMeshTriangulation triangulation = UnityEngine.AI.NavMesh.CalculateTriangulation();
        Debug.Log($"[BakeNavMesh] ✓ NavMesh baked successfully!");
        Debug.Log($"  Vertices: {triangulation.vertices.Length}");
        Debug.Log($"  Triangles: {triangulation.indices.Length / 3}");
        
        if (triangulation.vertices.Length > 0)
        {
            Vector3 min = triangulation.vertices[0];
            Vector3 max = triangulation.vertices[0];
            
            foreach (Vector3 vertex in triangulation.vertices)
            {
                min = Vector3.Min(min, vertex);
                max = Vector3.Max(max, vertex);
            }
            
            Vector3 size = max - min;
            Debug.Log($"  NavMesh Size: {size}");
        }
        
        EditorUtility.DisplayDialog("NavMesh Baked", 
            $"NavMesh baked successfully!\n\n" +
            $"Vertices: {triangulation.vertices.Length}\n" +
            $"Triangles: {triangulation.indices.Length / 3}\n" +
            $"Tree obstacles: {treeColliders}", 
            "OK");
    }
    
    [MenuItem("Tools/NavMesh/Check NavMesh Setup")]
    public static void CheckSetup()
    {
        Debug.Log("=== NAVMESH SETUP CHECK ===");
        
        // Check SubScenes
        SubScene[] subScenes = GameObject.FindObjectsOfType<SubScene>();
        Debug.Log($"SubScenes: {subScenes.Length}");
        foreach (SubScene subScene in subScenes)
        {
            Debug.Log($"  - {subScene.name}: {(subScene.IsLoaded ? "✓ Loaded" : "✗ NOT Loaded")}");
        }
        
        // Check NavMeshSurface
        NavMeshSurface surface = GameObject.FindObjectOfType<NavMeshSurface>();
        if (surface != null)
        {
            Debug.Log($"NavMeshSurface: ✓ Found on '{surface.gameObject.name}'");
            Debug.Log($"  Size: {surface.size}");
            Debug.Log($"  CollectObjects: {surface.collectObjects}");
            Debug.Log($"  UseGeometry: {surface.useGeometry}");
        }
        else
        {
            Debug.LogError("NavMeshSurface: ✗ NOT FOUND");
        }
    }
}
#endif

