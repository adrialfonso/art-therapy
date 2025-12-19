using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Holds all shared state and references for brush-related systems
public class BrushContext : MonoBehaviour
{
    [Header("XR Ray Interactors")]
    [SerializeField] public XRRayInteractor leftRay;
    [SerializeField] public XRRayInteractor rightRay;

    [Header("Draw Event Observers")]
    [SerializeField] public DrawEventObserver leftController;
    [SerializeField] public DrawEventObserver rightController;

    [Header("Brush Settings Observer")]
    [SerializeField] public BrushSettingsObserver brushSettings;
    
    [Header("Environment Settings Observer")]
    [SerializeField] public EnvironmentSettingsObserver environmentSettings;

    [Header("Audio Manager")]
    [SerializeField] public AudioSettings audioSettings;

    [Header("Skybox Options")]
    [SerializeField] public Material[] skyboxOptions;

    [Header("UI Canvases")]
    [SerializeField] public GameObject canvas2DUI;
    [SerializeField] public GameObject canvas3DUI;
    
    [Header("3D Mode")]
    [SerializeField] public Transform rightTipTransform;   
    [SerializeField] public Transform leftTipTransform; 
    [SerializeField] public LineRenderer linePrefab;

    // Drawing Mode & drawing state
    public bool is3DMode = false;
    public bool isErasing = false;
    public bool isDrawingLeft = false;
    public bool isDrawingRight = false;

    // Artwork persistence
    public int currentArtworkIndex = -1; 
    public string[] savedWhiteboardArtworks; 
    public Whiteboard whiteboard;
    public MessageLogger messageLogger;
}
