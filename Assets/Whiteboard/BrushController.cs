using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.IO;

// BrushController class with all common code
public class BrushController : MonoBehaviour
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

    [Header("Skybox Options")]
    [SerializeField] public Material[] skyboxOptions;

    [Header("Ambient Music Options")]
    [SerializeField] public AudioClip[] ambientMusicOptions;
    [SerializeField] public AudioSource ambientSource;
    
    [Header("3D Mode")]
    [SerializeField] public Transform rightTipTransform;   
    [SerializeField] public Transform leftTipTransform; 
    [SerializeField] public LineRenderer linePrefab;

    // 2D references
    public Color[] brushColors;
    public List<IMarkerStrategy> strategies;
    public IMarkerStrategy currentStrategy;
    public bool isErasing;

    // Mode & drawing state
    public bool is3DMode = false;
    public bool isDrawingLeft = false;
    public bool isDrawingRight = false;

    // Artwork persistence
    public int currentArtworkIndex = -1; 
    public string[] savedWhiteboardArtworks; 
    
    // Artwork handler
    public ArtworkHandler artworkHandler;

    public Light directionalLight;

    private void OnEnable()
    {   
        InitializeObserverSubscriptions();
    }

    private void Start()
    {
        // Initialize brush colors
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();

        // Find the directional light in the scene
        directionalLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        InitializeMarkerStrategies();
        SelectRandomSkybox();
        SelectRandomAmbientMusic();

        // Inicializa handler según modo
        artworkHandler = is3DMode ? (ArtworkHandler)new ArtworkHandler3D(this) : new ArtworkHandler2D(this);
    }

    private void Update()
    {
        // Delegate drawing to the artwork handler
        artworkHandler.HandleDrawing();
    }

    // Toggle between drawing and erasing modes (observer pattern)
    private void ToggleEraseMode()
    {
        isErasing = !isErasing;
    }

    // Toggle between 2D and 3D drawing modes (observer pattern)
    private void Toggle3DMode(bool active)
    {
        is3DMode = active;
        artworkHandler = is3DMode ? (ArtworkHandler)new ArtworkHandler3D(this) : new ArtworkHandler2D(this);
    }

    // Perform undo (observer pattern)
    private void Undo()
    {
        artworkHandler.Undo();
    }

    // Save current artwork to persistent storage (observer pattern)
    private void SaveArtwork()
    {
        artworkHandler.SaveArtwork();
    }

    // Load artworks from storage (observer pattern)
    private void LoadArtwork()
    {
        artworkHandler.LoadArtwork();
    }

    // Listener for brush size changes (observer pattern)
    private void OnBrushSizeChanged(int size)
    {
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, size * size).ToArray();
    }

    // Listener for brush color changes (observer pattern)
    private void OnBrushColorChanged(Color color)
    {
        brushColors = Enumerable.Repeat(color, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();
    }

    // Listener for strategy changes (observer pattern)
    private void OnStrategyChanged(int index)
    {
        if (index >= 0 && index < strategies.Count)
            currentStrategy = strategies[index];
    }

    // Listener for changes in the direct light of the environment
    private void OnDirectLightChanged(float intensity)
    {
        if (directionalLight != null)
            directionalLight.intensity = intensity;
    }

    // Listener for changes in the ambient light of the environment
    private void OnAmbientLightChanged(float intensity)
    {
        RenderSettings.ambientIntensity = intensity;
    }

    // Listener for scene load events to update directional light reference
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        directionalLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        OnDirectLightChanged(environmentSettings.DirectLightIntensity);
        OnAmbientLightChanged(environmentSettings.AmbientLightIntensity);
    }

    // Listener for changes in ambient volume of the environment
    private void OnAmbientVolumeChanged(float volume)
    {
        if (ambientSource != null)
            ambientSource.volume = volume;
    }

    // Select random skybox (button triggered)
    public void SelectRandomSkybox()
    {
        if (skyboxOptions == null || skyboxOptions.Length == 0) return;
        int index = Random.Range(0, skyboxOptions.Length);
        RenderSettings.skybox = skyboxOptions[index];
        DynamicGI.UpdateEnvironment();
    }

    // Select random ambient music (button triggered)
    public void SelectRandomAmbientMusic()
    {
        if (ambientMusicOptions == null || ambientMusicOptions.Length == 0) return;

        if (ambientSource == null)
            ambientSource = gameObject.AddComponent<AudioSource>();

        int index = Random.Range(0, ambientMusicOptions.Length);

        ambientSource.clip = ambientMusicOptions[index];
        ambientSource.loop = true;
        ambientSource.volume = 0.1f;
        ambientSource.playOnAwake = false;
        ambientSource.spatialBlend = 0f;
        ambientSource.Play();
    }

    // Initialize subscriptions to observer events
    private void InitializeObserverSubscriptions()
    {
        // Subscribe to brush settings changes
        if (brushSettings != null)
        {
            brushSettings.OnBrushSizeChanged += OnBrushSizeChanged;
            brushSettings.OnBrushColorChanged += OnBrushColorChanged;
            brushSettings.OnStrategyChanged += OnStrategyChanged;
            brushSettings.On3DModeChanged += Toggle3DMode;
            brushSettings.OnSaveArtwork += SaveArtwork;
            brushSettings.OnLoadArtwork += LoadArtwork;
        }

        if (leftController != null)
        {
            leftController.OnDrawPressed += () => isDrawingLeft = true;
            leftController.OnDrawReleased += () => isDrawingLeft = false;
            leftController.OnErasePressed += ToggleEraseMode;
            leftController.OnUndoPressed += Undo;
        }

        if (rightController != null)
        {
            rightController.OnDrawPressed += () => isDrawingRight = true;
            rightController.OnDrawReleased += () => isDrawingRight = false;
            rightController.OnErasePressed += ToggleEraseMode;
            rightController.OnUndoPressed += Undo;
        }

        if (environmentSettings != null)
        {
            environmentSettings.OnDirectLightChanged += OnDirectLightChanged;
            environmentSettings.OnAmbientLightChanged += OnAmbientLightChanged;
            environmentSettings.OnAmbientVolumeChanged += OnAmbientVolumeChanged;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // Initialize available marker strategies
    private void InitializeMarkerStrategies()
    {
        strategies = new List<IMarkerStrategy>
        {
            new NormalMarkerStrategy(),
            new GraffitiMarkerStrategy(),
            new WatercolorMarkerStrategy()
        };

        currentStrategy = strategies[brushSettings.StrategyIndex];
    }
}
