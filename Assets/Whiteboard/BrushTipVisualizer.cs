using UnityEngine;

public class BrushTipVisualizer : MonoBehaviour
{
    [Header("Tip Transform (Empty)")]
    [SerializeField] private Transform tip;

    [Header("Brush Settings Reference")]
    [SerializeField] private BrushSettingsObserver brushSettings;

    [Header("Brush Controller Reference")]
    [SerializeField] private BrushController brushController;

    [Header("LineRenderer Reference")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Sphere Size Settings")]
    [SerializeField] private float minSize = 0.005f;
    [SerializeField] private float maxSize = 0.05f;

    private GameObject tipSphere;
    private Renderer sphereRenderer;
    private bool previous3DMode;

    private void Start()
    {
        previous3DMode = brushController.is3DMode;
        tipSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        tipSphere.transform.SetParent(tip);
        tipSphere.transform.localPosition = Vector3.zero;
        tipSphere.transform.localRotation = Quaternion.identity;

        Destroy(tipSphere.GetComponent<Collider>());
        sphereRenderer = tipSphere.GetComponent<Renderer>();
        sphereRenderer.material = new Material(lineRenderer.material);

        tipSphere.SetActive(previous3DMode);
        UpdateSize();
        UpdateColor();
    }

    private void Update()
    {
        if (brushController.is3DMode != previous3DMode)
        {
            tipSphere.SetActive(brushController.is3DMode);
            previous3DMode = brushController.is3DMode;
        }

        if (!brushController.is3DMode) return;

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
