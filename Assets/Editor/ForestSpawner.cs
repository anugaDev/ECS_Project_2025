#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Unity.Scenes;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class ForestSpawner : EditorWindow
{
    private GameObject _treePrefab;

    private int _treeCount = 50;

    private float _spawnRadius = 80f;

    private Vector2 _spawnCenter = Vector2.zero;

    private float _minDistance = 5f;

    private bool _randomRotation = true;

    private Vector2 _scaleRange = new Vector2(0.8f, 1.2f);

    private SubScene _targetSubScene;

    [MenuItem("Tools/Forest Spawner")]
    public static void ShowWindow()
    {
        GetWindow<ForestSpawner>("Forest Spawner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Forest Spawner Settings", EditorStyles.boldLabel);
        GUILayout.Space(10);

        _treePrefab = (GameObject)EditorGUILayout.ObjectField("Tree Prefab", _treePrefab, typeof(GameObject), false);
        _targetSubScene = (SubScene)EditorGUILayout.ObjectField("Target SubScene", _targetSubScene, typeof(SubScene), true);
        
        GUILayout.Space(10);
        
        _treeCount = EditorGUILayout.IntSlider("Tree Count", _treeCount, 1, 500);
        _spawnRadius = EditorGUILayout.Slider("Spawn Radius", _spawnRadius, 10f, 200f);
        _spawnCenter = EditorGUILayout.Vector2Field("Spawn Center (X, Z)", _spawnCenter);
        _minDistance = EditorGUILayout.Slider("Min Distance Between Trees", _minDistance, 1f, 20f);
        
        GUILayout.Space(10);
        
        _randomRotation = EditorGUILayout.Toggle("Random Rotation", _randomRotation);
        _scaleRange = EditorGUILayout.Vector2Field("Scale Range (Min, Max)", _scaleRange);
        
        GUILayout.Space(20);

        if (GUILayout.Button("Spawn Forest", GUILayout.Height(40)))
        {
            SpawnForest();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Clear All Trees", GUILayout.Height(30)))
        {
            ClearTrees();
        }
    }

    private void SpawnForest()
    {
        if (_treePrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Tree Prefab!", "OK");
            return;
        }

        if (_targetSubScene == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Target SubScene!", "OK");
            return;
        }

        if (!_targetSubScene.IsLoaded)
        {
            EditorUtility.DisplayDialog("Error", "SubScene must be loaded! Right-click the SubScene and select 'Open Scene Additive'", "OK");
            return;
        }

        UnityEngine.SceneManagement.Scene subScene = _targetSubScene.EditingScene;
        if (!subScene.IsValid())
        {
            EditorUtility.DisplayDialog("Error", "SubScene editing scene is not valid!", "OK");
            return;
        }

        GameObject forestParent = new GameObject("Forest");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(forestParent, subScene);

        System.Collections.Generic.List<Vector2> spawnedPositions = new System.Collections.Generic.List<Vector2>();
        int spawnedCount = 0;
        int maxAttempts = _treeCount * 10;
        int attempts = 0;

        while (spawnedCount < _treeCount && attempts < maxAttempts)
        {
            attempts++;

            Vector2 randomPos = Random.insideUnitCircle * _spawnRadius;
            randomPos += _spawnCenter;

            bool tooClose = false;
            foreach (Vector2 existingPos in spawnedPositions)
            {
                if (Vector2.Distance(randomPos, existingPos) < _minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (tooClose)
                continue;

            Vector3 spawnPosition = new Vector3(randomPos.x, 0f, randomPos.y);

            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(_treePrefab, forestParent.transform);
            tree.transform.position = spawnPosition;

            if (_randomRotation)
            {
                tree.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            }

            float randomScale = Random.Range(_scaleRange.x, _scaleRange.y);
            tree.transform.localScale = Vector3.one * randomScale;

            tree.name = $"Tree_{spawnedCount}";

            spawnedPositions.Add(randomPos);
            spawnedCount++;
        }

        EditorUtility.SetDirty(forestParent);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(subScene);

        Debug.Log($"Spawned {spawnedCount} trees in SubScene '{_targetSubScene.name}'");
    }

    private void ClearTrees()
    {
        if (_targetSubScene == null)
        {
            EditorUtility.DisplayDialog("Error", "Please assign a Target SubScene!", "OK");
            return;
        }

        if (!_targetSubScene.IsLoaded)
        {
            EditorUtility.DisplayDialog("Error", "SubScene must be loaded!", "OK");
            return;
        }

        UnityEngine.SceneManagement.Scene subScene = _targetSubScene.EditingScene;
        if (!subScene.IsValid())
        {
            return;
        }

        foreach (GameObject rootObj in subScene.GetRootGameObjects())
        {
            if (rootObj.name == "Forest")
            {
                DestroyImmediate(rootObj);
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(subScene);
                Debug.Log("Cleared all trees from SubScene");
                return;
            }
        }
    }
}
#endif

