using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;

// Controller for brush interactions and settings
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

    [Header("Audio Manager")]
    [SerializeField] private AudioSettings audioSettings;

    [Header("Skybox Options")]
    [SerializeField] public Material[] skyboxOptions;

    [Header("UI Canvases")]
    [SerializeField] private GameObject canvas2DUI;
    [SerializeField] private GameObject canvas3DUI;
    
    [Header("3D Mode")]
    [SerializeField] public Transform rightTipTransform;   
    [SerializeField] public Transform leftTipTransform; 
    [SerializeField] public LineRenderer linePrefab;

    // Drawing Mode & drawing state
    public bool is3DMode = false;
    public bool isDrawingLeft = false;
    public bool isDrawingRight = false;

    // Artwork persistence
    public int currentArtworkIndex = -1; 
    public string[] savedWhiteboardArtworks; 
    
    // Artwork handler 2D/3D
    public Whiteboard whiteboard;
    public ArtworkHandler artworkHandler;
    public MessageLogger messageLogger;

    private void OnEnable()
    {   
        InitializeObserverSubscriptions();
    }

    private void Start()
    {
        // Initialize environment
        audioSettings.PlayRandomAmbientMusic();
        SelectRandomSkybox();
        OnSkyExposureChanged(environmentSettings.SkyExposure);
        OnSkyRotationChanged(environmentSettings.SkyRotation);
        OnAmbientVolumeChanged(environmentSettings.AmbientVolume);

        Toggle3DMode(is3DMode);
        messageLogger.Log("Welcome to Art Therapy VR! Start creating your masterpiece.");
    }

    private void Update()
    {
        artworkHandler.HandleDrawing();
    }

    // Toggle between drawing and erasing modes (observer pattern) (delegated to ArtworkHandler2D)
    private void ToggleEraseMode()
    {
        if (artworkHandler is ArtworkHandler2D handler2D)
            handler2D.ToggleEraseMode();

        audioSettings.PlaySoundEffect("snap");
    }

    // Toggle between 2D and 3D drawing modes (observer pattern) 
    private void Toggle3DMode(bool active)
    {
        is3DMode = active;

        // Hide/Show whiteboard based on mode
        if (is3DMode)
        {
            artworkHandler = new ArtworkHandler3D(this);
            messageLogger.Log("3D Brush mode");
            whiteboard.gameObject.SetActive(false);
        }
        else
        {
            artworkHandler = new ArtworkHandler2D(this);
            messageLogger.Log("2D Whiteboard mode");
            whiteboard.gameObject.SetActive(true);
        }

        canvas2DUI.SetActive(!is3DMode);
        canvas3DUI.SetActive(is3DMode);
    }

    // Perform undo (observer pattern)
    private void Undo()
    {
        artworkHandler.Undo();
        audioSettings.PlaySoundEffect("snap");
    }

    // Save current artwork to persistent data path (observer pattern)
    private void SaveArtwork()
    {
        artworkHandler.SaveArtwork();
    }

    // Load artworks from persistent data path (observer pattern)
    private void LoadArtwork()
    {
        artworkHandler.LoadArtwork();
    }

    // Create new artwork (observer pattern)
    private void NewArtwork()
    {
        artworkHandler.NewArtwork();
    }

    // Delete current artwork (observer pattern)
    private void DeleteArtwork()
    {
        artworkHandler.DeleteArtwork();
    }

    // Listener for brush size changes (observer pattern) (delegated to ArtworkHandler2D)
    private void OnBrushSizeChanged(int size)
    {
        if (artworkHandler is ArtworkHandler2D handler2D)
            handler2D.OnBrushSizeChanged(size);
    }

    // Listener for brush color changes (observer pattern) (delegated to ArtworkHandler2D)
    private void OnBrushColorChanged(Color color)
    {
        if (artworkHandler is ArtworkHandler2D handler2D)
            handler2D.OnBrushColorChanged(color);
    }

    // Listener for strategy changes (observer pattern) (delegated to ArtworkHandler2D)
    private void OnStrategyChanged(int index)
    {
        if (artworkHandler is ArtworkHandler2D handler2D)
            handler2D.OnStrategyChanged(index);
    }

    // Listener for whiteboard width changes (observer pattern)
    private void OnWhiteboardWidthChanged(float width)
    {
        if (whiteboard != null)
        {
            Vector3 scale = whiteboard.transform.localScale;
            scale.x = width;
            whiteboard.transform.localScale = scale;
        }
    }

    // Listener for whiteboard height changes (observer pattern)
    private void OnWhiteboardHeightChanged(float height)
    {
        if (whiteboard != null)
        {
            Vector3 scale = whiteboard.transform.localScale;
            scale.z = height;
            whiteboard.transform.localScale = scale;
        }
    }

    // Listener for changes in the sky exposure of the environment (observer pattern)
    private void OnSkyExposureChanged(float exposure)
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Exposure", exposure);
            DynamicGI.UpdateEnvironment();
        }
    }

    // Listener for changes in the sky rotation of the environment (observer pattern)
    private void OnSkyRotationChanged(float rotation)
    {
        if (RenderSettings.skybox != null)
        {
            RenderSettings.skybox.SetFloat("_Rotation", rotation);
            DynamicGI.UpdateEnvironment();
        }
    }

    // Listener for changes in ambient volume of the environment (observer pattern)
    private void OnAmbientVolumeChanged(float volume)
    {
        audioSettings.SetAmbientVolume(volume);
    }

    // Select random skybox (button triggered)
    public void SelectRandomSkybox()
    {
        if (skyboxOptions == null || skyboxOptions.Length == 0) return;
        int index = Random.Range(0, skyboxOptions.Length);
        RenderSettings.skybox = skyboxOptions[index];
        DynamicGI.UpdateEnvironment();
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
            brushSettings.OnNewArtwork += NewArtwork;
            brushSettings.OnDeleteArtwork += DeleteArtwork;
            brushSettings.OnWhiteboardWidthChanged += OnWhiteboardWidthChanged;
            brushSettings.OnWhiteboardHeightChanged += OnWhiteboardHeightChanged;
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
            environmentSettings.OnSkyExposureChanged += OnSkyExposureChanged;
            environmentSettings.OnSkyRotationChanged += OnSkyRotationChanged;
            environmentSettings.OnAmbientVolumeChanged += OnAmbientVolumeChanged;
        }
    }
}
