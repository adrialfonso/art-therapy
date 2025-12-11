using UnityEngine;

public class BrushTipVisualizer : MonoBehaviour
{
    [Header("Tip Transform (Empty)")]
    [SerializeField] private Transform tip;

    [Header("Brush Settings Reference")]
    [SerializeField] private BrushSettingsObserver brushSettings;

    [Header("LineRenderer Reference")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Sphere Size Settings")]
    [SerializeField] private float minSize = 0.01f;
    [SerializeField] private float maxSize = 0.05f;

    private GameObject tipSphere;
    private Renderer sphereRenderer;

    private void Start()
    {
        tipSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tipSphere.transform.SetParent(tip);
        tipSphere.transform.localPosition = Vector3.zero;
        tipSphere.transform.localRotation = Quaternion.identity;

        Destroy(tipSphere.GetComponent<Collider>());
        sphereRenderer = tipSphere.GetComponent<Renderer>();
        sphereRenderer.material = lineRenderer.material;

        UpdateSize();
        UpdateColor();
    }

    private void Update()
    {
        UpdateSize();
        UpdateColor();
    }

    private void UpdateSize()
    {
        // Interpolate size based on brush size (1-100)
        float normalized = Mathf.InverseLerp(1f, 100f, brushSettings.BrushSize);
        float size = Mathf.Lerp(minSize, maxSize, normalized);
        tipSphere.transform.localScale = Vector3.one * size;
    }

    private void UpdateColor()
    {
        sphereRenderer.material.color = brushSettings.BrushColor;
    }
}
