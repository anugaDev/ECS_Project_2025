using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ChildMeshInstanceCollector))]
public class ChildMeshInstanceCollectorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ChildMeshInstanceCollector collector =
            (ChildMeshInstanceCollector)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Compile meshes"))
        {
            collector.CollectMeshInstances();
        }
    }
}