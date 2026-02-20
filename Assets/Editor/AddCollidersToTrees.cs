#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AddCollidersToTrees : EditorWindow
{
    [MenuItem("Tools/NavMesh/Check Tree Colliders")]
    public static void CheckTreeColliders()
    {
        // Check prefab
        string[] guids = AssetDatabase.FindAssets("t:Prefab TreeResource");

        if (guids.Length == 0)
        {
            Debug.LogError("[CheckTreeColliders] TreeResource prefab not found!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        Debug.Log("=== TREE COLLIDER CHECK ===");
        Debug.Log($"Prefab: {path}");

        CapsuleCollider prefabCollider = prefab.GetComponent<CapsuleCollider>();
        if (prefabCollider != null)
        {
            Debug.Log($"✓ Prefab HAS CapsuleCollider: Radius={prefabCollider.radius}, Height={prefabCollider.height}");
        }
        else
        {
            Debug.LogWarning("✗ Prefab MISSING CapsuleCollider!");
        }

        // Check scene trees - only count root tree objects with ResourceAuthoring
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        int treeCount = 0;
        int treesWithCollider = 0;
        int treesWithPhysicsShape = 0;

        foreach (GameObject obj in allObjects)
        {
            // Only count objects that have ResourceAuthoring (actual tree entities)
            if (obj.GetComponent<GatherableResources.ResourceAuthoring>() != null)
            {
                treeCount++;

                if (obj.GetComponent<CapsuleCollider>() != null)
                {
                    treesWithCollider++;
                }

                if (obj.GetComponent<Unity.Physics.Authoring.PhysicsShapeAuthoring>() != null)
                {
                    treesWithPhysicsShape++;
                }
            }
        }

        Debug.Log($"\nScene Trees (ResourceAuthoring): {treeCount}");
        Debug.Log($"Trees with CapsuleCollider: {treesWithCollider}");
        Debug.Log($"Trees with PhysicsShapeAuthoring: {treesWithPhysicsShape}");

        if (treeCount > 0 && treesWithCollider == 0)
        {
            Debug.LogWarning("⚠ NO trees have Unity Colliders! Run 'Add Colliders to Trees' first!");
        }
        else if (treesWithCollider < treeCount)
        {
            Debug.LogWarning($"⚠ Only {treesWithCollider}/{treeCount} trees have Unity Colliders!");
            Debug.LogWarning($"   Run: Tools → NavMesh → Update SubScene Trees from Prefab");
        }
        else if (treesWithCollider > 0)
        {
            Debug.Log($"✓ All {treesWithCollider} trees have Unity Colliders!");
        }

        Debug.Log("\n💡 NavMesh uses Unity Colliders (CapsuleCollider), NOT ECS Physics Shapes!");
        Debug.Log("   Trees need BOTH for NavMesh + ECS physics to work together.");
    }

    [MenuItem("Tools/NavMesh/Add Colliders to Trees")]
    public static void AddColliders()
    {
        // Find the TreeResource prefab
        string[] guids = AssetDatabase.FindAssets("t:Prefab TreeResource");

        if (guids.Length == 0)
        {
            Debug.LogError("[AddCollidersToTrees] TreeResource prefab not found!");
            return;
        }

        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError($"[AddCollidersToTrees] Failed to load prefab at {path}");
            return;
        }

        Debug.Log($"[AddCollidersToTrees] Found TreeResource prefab at: {path}");

        // Check if it already has a collider
        CapsuleCollider existingCollider = prefab.GetComponent<CapsuleCollider>();
        if (existingCollider != null)
        {
            Debug.Log("[AddCollidersToTrees] Tree prefab already has a CapsuleCollider!");
            Debug.Log("  Now run: Tools → NavMesh → Update SubScene Trees from Prefab");
            return;
        }

        // Add CapsuleCollider to match the Physics Shape
        GameObject prefabInstance = PrefabUtility.LoadPrefabContents(path);
        CapsuleCollider collider = prefabInstance.AddComponent<CapsuleCollider>();

        // Match the Physics Shape settings (Cylinder with Height 1.57, Radius 0.36)
        collider.radius = 0.36f;
        collider.height = 1.57f;
        collider.center = new Vector3(0, 0.83f, 0);
        collider.direction = 2; // Z-axis (matches the Physics Shape)

        Debug.Log("[AddCollidersToTrees] ✓ Added CapsuleCollider to TreeResource prefab");
        Debug.Log($"  Radius: {collider.radius}, Height: {collider.height}, Center: {collider.center}");

        PrefabUtility.SaveAsPrefabAsset(prefabInstance, path);
        PrefabUtility.UnloadPrefabContents(prefabInstance);

        AssetDatabase.Refresh();

        Debug.Log("[AddCollidersToTrees] ✓ Done! Now run:");
        Debug.Log("  Tools → NavMesh → Update SubScene Trees from Prefab");
    }
    
    [MenuItem("Tools/NavMesh/Update SubScene Trees from Prefab")]
    public static void UpdateSubSceneTrees()
    {
        // Find all SubScenes in the project
        Unity.Scenes.SubScene[] subScenes = GameObject.FindObjectsOfType<Unity.Scenes.SubScene>();

        if (subScenes.Length == 0)
        {
            Debug.LogError("[AddCollidersToTrees] No SubScenes found in the scene!");
            return;
        }

        int totalUpdated = 0;

        foreach (Unity.Scenes.SubScene subScene in subScenes)
        {
            if (!subScene.IsLoaded)
            {
                Debug.LogWarning($"[AddCollidersToTrees] SubScene '{subScene.name}' is not loaded. Skipping...");
                continue;
            }

            UnityEngine.SceneManagement.Scene editingScene = subScene.EditingScene;
            if (!editingScene.IsValid())
            {
                Debug.LogWarning($"[AddCollidersToTrees] SubScene '{subScene.name}' editing scene is not valid. Skipping...");
                continue;
            }

            Debug.Log($"[AddCollidersToTrees] Processing SubScene: {subScene.name}");

            // Get all root objects in the SubScene
            GameObject[] rootObjects = editingScene.GetRootGameObjects();
            int updatedInScene = 0;

            foreach (GameObject rootObj in rootObjects)
            {
                // Find all tree instances (including children)
                Transform[] allTransforms = rootObj.GetComponentsInChildren<Transform>(true);

                foreach (Transform t in allTransforms)
                {
                    if (t.gameObject.name.Contains("Tree") || t.gameObject.name.Contains("tree"))
                    {
                        // Check if it's a prefab instance
                        if (PrefabUtility.IsPartOfPrefabInstance(t.gameObject))
                        {
                            PrefabUtility.RevertPrefabInstance(t.gameObject, InteractionMode.AutomatedAction);
                            updatedInScene++;
                        }
                    }
                }
            }

            if (updatedInScene > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(editingScene);
                Debug.Log($"[AddCollidersToTrees] ✓ Updated {updatedInScene} trees in SubScene '{subScene.name}'");
                totalUpdated += updatedInScene;
            }
        }

        Debug.Log($"[AddCollidersToTrees] ✓ Total updated: {totalUpdated} trees");
        Debug.Log("[AddCollidersToTrees] Now:");
        Debug.Log("  1. Save the SubScene");
        Debug.Log("  2. Make sure SubScene is LOADED (right-click → Open Scene Additive)");
        Debug.Log("  3. Rebake NavMesh: Window → AI → Navigation → Bake");
    }
}
#endif

