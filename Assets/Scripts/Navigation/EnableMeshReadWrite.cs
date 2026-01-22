#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace Navigation
{
    public class EnableMeshReadWrite : MonoBehaviour
    {
        [MenuItem("Tools/NavMesh/Fix NavMeshSurface Settings")]
        public static void FixNavMeshSurfaceSettings()
        {
            NavMeshSurface[] surfaces = FindObjectsOfType<NavMeshSurface>();

            if (surfaces.Length == 0)
            {
                Debug.LogWarning("[NavMeshFix] No NavMeshSurface found in scene!");
                return;
            }

            foreach (NavMeshSurface surface in surfaces)
            {
                Debug.Log($"[NavMeshFix] Found NavMeshSurface on: {surface.gameObject.name}");
                Debug.Log($"  Current useGeometry: {surface.useGeometry}");

                // FORCE PhysicsColliders mode
                surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.collectObjects = CollectObjects.All;
                surface.layerMask = ~0;
                surface.overrideVoxelSize = true;
                surface.voxelSize = 0.1f;

                EditorUtility.SetDirty(surface);

                Debug.Log($"[NavMeshFix] ✓ Fixed NavMeshSurface on: {surface.gameObject.name}");
                Debug.Log($"  New useGeometry: {surface.useGeometry}");
            }

            Debug.Log($"[NavMeshFix] Fixed {surfaces.Length} NavMeshSurface(s). Press Play to test!");
        }
        [MenuItem("Tools/NavMesh/Enable Read Write on All Meshes")]
        public static void EnableReadWriteOnAllMeshes()
        {
            // Find all model assets
            string[] guids = AssetDatabase.FindAssets("t:Model");
            int fixedCount = 0;
            int totalCount = guids.Length;

            Debug.Log($"[EnableMeshReadWrite] Found {totalCount} model assets. Checking...");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer != null && !importer.isReadable)
                {
                    importer.isReadable = true;
                    importer.SaveAndReimport();
                    fixedCount++;
                    Debug.Log($"[EnableMeshReadWrite] ✓ Enabled Read/Write on: {path}");
                }
            }

            Debug.Log($"[EnableMeshReadWrite] Done! Fixed {fixedCount}/{totalCount} models.");
            
            if (fixedCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log("[EnableMeshReadWrite] Please rebuild NavMesh for changes to take effect.");
            }
        }

        [MenuItem("Tools/NavMesh/Check Building Meshes")]
        public static void CheckBuildingMeshes()
        {
            // Find all GameObjects with "Building" or "tower" in name
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
            int readableCount = 0;
            int notReadableCount = 0;

            Debug.Log("[EnableMeshReadWrite] Checking building meshes...");

            foreach (GameObject obj in allObjects)
            {
                if (obj.name.ToLower().Contains("building") || 
                    obj.name.ToLower().Contains("tower") ||
                    obj.name.ToLower().Contains("house"))
                {
                    MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        if (meshFilter.sharedMesh.isReadable)
                        {
                            readableCount++;
                            Debug.Log($"  ✓ {obj.name}: Read/Write ENABLED");
                        }
                        else
                        {
                            notReadableCount++;
                            Debug.LogWarning($"  ✗ {obj.name}: Read/Write DISABLED - NavMesh can't use this!");
                        }
                    }
                }
            }

            Debug.Log($"[EnableMeshReadWrite] Results:\n" +
                     $"  Readable: {readableCount}\n" +
                     $"  Not Readable: {notReadableCount}");

            if (notReadableCount > 0)
            {
                Debug.LogWarning("[EnableMeshReadWrite] Run 'Tools → NavMesh → Enable Read Write on All Meshes' to fix!");
            }
        }
    }
}
#endif

