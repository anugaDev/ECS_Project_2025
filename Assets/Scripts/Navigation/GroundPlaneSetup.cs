using UnityEngine;
using Unity.AI.Navigation;

namespace Navigation
{
    public class GroundPlaneSetup : MonoBehaviour
    {
        public static GroundPlaneSetup Instance { get; private set; }

        [Header("Ground Settings")]
        [SerializeField]
        private Vector3 _size;
        
        [SerializeField]
        private bool _createOnAwake = true;

        [SerializeField]
        private bool _showVisual = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (_createOnAwake)
            {
                CreateGroundPlane();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        [ContextMenu("Create Ground Plane")]
        public void CreateGroundPlane()
        {
            BoxCollider existingCollider = GetComponent<BoxCollider>();
            if (existingCollider != null)
            {
                return;
            }

            BoxCollider collider = gameObject.AddComponent<BoxCollider>();
            collider.size = _size;
            collider.center = Vector3.zero;
            collider.isTrigger = false;

            NavMeshModifier modifier = gameObject.AddComponent<NavMeshModifier>();
            modifier.overrideArea = true;
            modifier.area = 0;

            transform.position = new Vector3(0, -_size.y / 2f, 0);

            if (_showVisual)
            {
                CreateVisual();
            }
        }

        private void CreateVisual()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Plane);
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(_size.x / 10f, 1f, _size.z / 10f);

            Renderer renderer = visual.GetComponent<Renderer>();
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0, 1, 0, 0.3f);
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;

            Destroy(visual.GetComponent<MeshCollider>());
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawCube(transform.position, _size);
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, _size);
        }
    }
}

