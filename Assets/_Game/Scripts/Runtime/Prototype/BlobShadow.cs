using UnityEngine;

namespace BoozeBlocks.Prototype
{
    [DisallowMultipleComponent]
    public sealed class BlobShadow : MonoBehaviour
    {
        private const int GroundLayerMask = ~0;

        [SerializeField, Min(0.1f)] private float radius = 1.1f;
        [SerializeField, Range(0f, 1f)] private float maxAlpha = 0.55f;
        [SerializeField, Min(0f)] private float maxHeight = 3f;
        [SerializeField, Min(0f)] private float verticalOffset = 0.02f;
        [SerializeField, Min(0f)] private float rayOrigin = 1.5f;

        private static Material sharedMaterial;
        private static readonly int ColorProperty = Shader.PropertyToID("_Color");

        private Transform quad;
        private MeshRenderer quadRenderer;
        private MaterialPropertyBlock properties;

        private void Awake()
        {
            EnsureMaterial();
            BuildQuad();
        }

        private void OnDestroy()
        {
            if (quad != null) Destroy(quad.gameObject);
        }

        private void LateUpdate()
        {
            if (quad == null) return;
            Vector3 origin = transform.position + Vector3.up * rayOrigin;
            float height;
            Vector3 normal = Vector3.up;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayOrigin + maxHeight,
                    GroundLayerMask, QueryTriggerInteraction.Ignore))
            {
                height = transform.position.y - hit.point.y;
                normal = hit.normal;
                quad.position = hit.point + normal * verticalOffset;
            }
            else
            {
                height = maxHeight;
                quad.position = transform.position + Vector3.down * (transform.position.y - verticalOffset);
            }

            quad.rotation = Quaternion.LookRotation(Vector3.Cross(normal, transform.right), normal);
            float fade = Mathf.Clamp01(1f - height / maxHeight);
            float alpha = maxAlpha * fade * fade;
            if (properties == null) properties = new MaterialPropertyBlock();
            quadRenderer.GetPropertyBlock(properties);
            properties.SetColor(ColorProperty, new Color(0f, 0f, 0f, alpha));
            quadRenderer.SetPropertyBlock(properties);
        }

        private static void EnsureMaterial()
        {
            if (sharedMaterial != null) return;
            Shader shader = Shader.Find("BoozeBlocks/BlobShadow");
            if (shader == null) return;
            sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
        }

        private void BuildQuad()
        {
            if (sharedMaterial == null) return;
            GameObject quadObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quadObject.name = "Blob Shadow";
            Destroy(quadObject.GetComponent<Collider>());
            quad = quadObject.transform;
            quad.localScale = Vector3.one * (radius * 2f);
            quadRenderer = quadObject.GetComponent<MeshRenderer>();
            quadRenderer.sharedMaterial = sharedMaterial;
            quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            quadRenderer.receiveShadows = false;
            quadRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            quadRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }
}
