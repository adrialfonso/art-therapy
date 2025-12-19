using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Controller for brush interactions and settings
public class BrushController : MonoBehaviour
{
    [Header("Context")]
    [SerializeField] private BrushContext context;

    // Artwork handler 2D/3D
    private ArtworkHandler2D handler2DInstance;
    private ArtworkHandler3D handler3DInstance;
    public ArtworkHandler artworkHandler;

    private void OnEnable()
    {   
        InitializeObserverSubscriptions();
    }

    private void Start()
    {
        InitialSetup();
    }

    private void Update()
    {
        artworkHandler.HandleDrawing();
    }

    // Toggle between drawing and erasing modes (observer pattern) (delegated to ArtworkHandler2D)
    private void ToggleEraseMode()
    {
        artworkHandler.ToggleEraseMode();
        context.audioSettings.PlaySoundEffect("snap");
    }

    // Toggle between 2D and 3D drawing modes (observer pattern) 
    private void Toggle3DMode(bool active)
    {
        context.is3DMode = active;

        // Hide/Show whiteboard based on mode
        if (context.is3DMode)
        {
            artworkHandler = handler3DInstance;
            context.messageLogger.Log("3D Brush mode");
            context.whiteboard.gameObject.SetActive(false);
        }
        else
        {
            artworkHandler = handler2DInstance;
            context.messageLogger.Log("2D Whiteboard mode");
            context.whiteboard.gameObject.SetActive(true);
        }

        context.canvas2DUI.SetActive(!context.is3DMode);
        context.canvas3DUI.SetActive(context.is3DMode);
    }

    // Perform undo (observer pattern)
    private void Undo()
    {
        artworkHandler.Undo();
        context.audioSettings.PlaySoundEffect("snap");
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
        if (context.whiteboard != null)
        {
            Vector3 scale = context.whiteboard.transform.localScale;
            scale.x = width;
            context.whiteboard.transform.localScale = scale;
        }
    }

    // Listener for whiteboard height changes (observer pattern)
    private void OnWhiteboardHeightChanged(float height)
    {
        if (context.whiteboard != null)
        {
            Vector3 scale = context.whiteboard.transform.localScale;
            scale.z = height;
            context.whiteboard.transform.localScale = scale;
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
        context.audioSettings.SetAmbientVolume(volume);
    }

    // Select random skybox (button triggered)
    public void SelectRandomSkybox()
    {
        if (context.skyboxOptions == null || context.skyboxOptions.Length == 0) return;
        int index = Random.Range(0, context.skyboxOptions.Length);
        RenderSettings.skybox = context.skyboxOptions[index];
        DynamicGI.UpdateEnvironment();
    }

    // Initialize subscriptions to observer events
    private void InitializeObserverSubscriptions()
    {
        // Subscribe to brush settings changes
        if (context.brushSettings != null)
        {
            context.brushSettings.OnBrushSizeChanged += OnBrushSizeChanged;
            context.brushSettings.OnBrushColorChanged += OnBrushColorChanged;
            context.brushSettings.OnStrategyChanged += OnStrategyChanged;
            context.brushSettings.On3DModeChanged += Toggle3DMode;
            context.brushSettings.OnSaveArtwork += SaveArtwork;
            context.brushSettings.OnLoadArtwork += LoadArtwork;
            context.brushSettings.OnNewArtwork += NewArtwork;
            context.brushSettings.OnDeleteArtwork += DeleteArtwork;
            context.brushSettings.OnWhiteboardWidthChanged += OnWhiteboardWidthChanged;
            context.brushSettings.OnWhiteboardHeightChanged += OnWhiteboardHeightChanged;
        }

        if (context.leftController != null)
        {
            context.leftController.OnDrawPressed += () => context.isDrawingLeft = true;
            context.leftController.OnDrawReleased += () => context.isDrawingLeft = false;
            context.leftController.OnErasePressed += ToggleEraseMode;
            context.leftController.OnUndoPressed += Undo;
        }

        if (context.rightController != null)
        {
            context.rightController.OnDrawPressed += () => context.isDrawingRight = true;
            context.rightController.OnDrawReleased += () => context.isDrawingRight = false;
            context.rightController.OnErasePressed += ToggleEraseMode;
            context.rightController.OnUndoPressed += Undo;
        }

        if (context.environmentSettings != null)
        {
            context.environmentSettings.OnSkyExposureChanged += OnSkyExposureChanged;
            context.environmentSettings.OnSkyRotationChanged += OnSkyRotationChanged;
            context.environmentSettings.OnAmbientVolumeChanged += OnAmbientVolumeChanged;
        }
    }

    // Initialize environment
    private void InitialSetup()
    {
        context.audioSettings.PlayRandomAmbientMusic();
        SelectRandomSkybox();
        OnSkyExposureChanged(context.environmentSettings.SkyExposure);
        OnSkyRotationChanged(context.environmentSettings.SkyRotation);
        OnAmbientVolumeChanged(context.environmentSettings.AmbientVolume);

        handler2DInstance = new ArtworkHandler2D(context);
        handler3DInstance = new ArtworkHandler3D(context);

        handler3DInstance.LoadArtwork();
        Toggle3DMode(context.is3DMode);
        context.messageLogger.Log("Welcome to Art Therapy VR! Start creating your masterpiece.");
    }
}
