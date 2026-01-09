using UnityEngine;
using System.Collections.Generic;

public class ChildMeshInstanceCollector : MonoBehaviour
{
    [Header("Mesh Instances")]
    public List<MeshFilter> meshFilters = new List<MeshFilter>();

    public List<SkinnedMeshRenderer> skinnedMeshRenderers = new List<SkinnedMeshRenderer>();

    public void CollectMeshInstances()
    {
        meshFilters.Clear();
        skinnedMeshRenderers.Clear();

        meshFilters.AddRange(GetComponentsInChildren<MeshFilter>(true));
        skinnedMeshRenderers.AddRange(GetComponentsInChildren<SkinnedMeshRenderer>(true));

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log(
            $"MeshFilters: {meshFilters.Count} | " +
            $"SkinnedMeshRenderers: {skinnedMeshRenderers.Count}"
        );
    }
}