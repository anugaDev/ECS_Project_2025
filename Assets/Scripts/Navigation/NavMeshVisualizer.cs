using UnityEngine;
using UnityEngine.AI;

namespace Navigation
{
    public class NavMeshVisualizer : MonoBehaviour
    {
        private bool _showNavMesh = false;
        private Mesh _navMeshVisualization;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                _showNavMesh = !_showNavMesh;

                if (_showNavMesh)
                {
                    GenerateNavMeshVisualization();
                }
            }
        }

        private void GenerateNavMeshVisualization()
        {
            NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

            if (triangulation.vertices.Length == 0)
            {
                return;
            }

            _navMeshVisualization = new Mesh();
            _navMeshVisualization.vertices = triangulation.vertices;
            _navMeshVisualization.triangles = triangulation.indices;
            _navMeshVisualization.RecalculateNormals();
        }

        private void OnDrawGizmos()
        {
            if (!_showNavMesh || _navMeshVisualization == null) return;

            Gizmos.color = new Color(0, 1, 0, 0.3f);

            Vector3[] vertices = _navMeshVisualization.vertices;
            int[] triangles = _navMeshVisualization.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v0 = vertices[triangles[i]];
                Vector3 v1 = vertices[triangles[i + 1]];
                Vector3 v2 = vertices[triangles[i + 2]];

                Gizmos.DrawLine(v0, v1);
                Gizmos.DrawLine(v1, v2);
                Gizmos.DrawLine(v2, v0);
            }
        }
    }
}

