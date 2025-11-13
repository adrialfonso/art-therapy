using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;

// Controller to manage brush drawing on the whiteboard using XRControllers
public class BrushController : MonoBehaviour
{
    [Header("XR Ray Interactors")]
    [SerializeField] private XRRayInteractor leftRay;
    [SerializeField] private XRRayInteractor rightRay;

    [Header("Draw Event Observers")]
    [SerializeField] private DrawEventObserver leftController;
    [SerializeField] private DrawEventObserver rightController;

    [Header("Brush Settings Observer")]
    [SerializeField] private BrushSettingsObserver brushSettings;
    [SerializeField] private EnvironmentSettingsObserver environmentSettings;

    private Light directionalLight;
    private Whiteboard whiteboard;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private IMarkerStrategy currentStrategy;
    private bool isErasing;

    public XRControllerState leftState;
    public XRControllerState rightState;

    private void OnEnable()
    {   
        InitializeObserverSubscriptions();
    }

    private void Start()
    {
        // Initialize controller states
        leftState = new XRControllerState { ray = leftRay };
        rightState = new XRControllerState { ray = rightRay };

        // Initialize brush colors
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();

        // Find the directional light in the scene
        directionalLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        InitializeMarkerStrategies();
    }

    private void Update()
    {
        HandleDrawing(leftState);
        HandleDrawing(rightState);
    }

    // Handle drawing logic for a given controller state
    private void HandleDrawing(XRControllerState state)
    {
        // If not drawing, reset state and return
        if (!state.isDrawing)
        {
            state.touchedLastFrame = false;
            state.savedUndoState = false;
            return;
        }

        // Perform raycast and handle drawing
        if (state.ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            // Get or cache the whiteboard reference
            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != whiteboard)
            {
                whiteboard = hitBoard;
                state.savedUndoState = false;
            }

            // Calculate touch position on the whiteboard texture
            Vector2 touchPos = new Vector2(hit.textureCoord.x * whiteboard.textureSize.x, hit.textureCoord.y * whiteboard.textureSize.y);

            // Save undo state if not already saved for this drawing session
            if (!state.savedUndoState)
            {
                whiteboard.SaveUndoState();
                state.savedUndoState = true;
            }

            // Draw using the current strategy
            if (state.touchedLastFrame)
                currentStrategy.Draw(whiteboard, touchPos, state.lastTouchPos, brushSettings.BrushSize, brushColors, isErasing);

            state.lastTouchPos = touchPos;
            state.touchedLastFrame = true;
        }
        else
        {
            state.touchedLastFrame = false;
        }
    }

    // Toggle between drawing and erasing modes (observer pattern)
    private void ToggleEraseMode()
    {
        isErasing = !isErasing;
    }

    // Perform undo on the whiteboard (observer pattern)
    private void UndoWhiteboard()
    {
        if (whiteboard != null)
            whiteboard.Undo();
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
        FindDirectionalLight();
    }

    // Find and update the directional light reference in the scene
    private void FindDirectionalLight()
    {
        directionalLight = FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);

        OnDirectLightChanged(environmentSettings.DirectLightIntensity);
        OnAmbientLightChanged(environmentSettings.AmbientLightIntensity);
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
        }

        if (leftController != null)
        {
            leftController.OnDrawPressed += () => leftState.isDrawing = true;
            leftController.OnDrawReleased += () => leftState.isDrawing = false;
            leftController.OnErasePressed += ToggleEraseMode;
            leftController.OnUndoPressed += UndoWhiteboard;
        }

        if (rightController != null)
        {
            rightController.OnDrawPressed += () => rightState.isDrawing = true;
            rightController.OnDrawReleased += () => rightState.isDrawing = false;
            rightController.OnErasePressed += ToggleEraseMode;
            rightController.OnUndoPressed += UndoWhiteboard;
        }

        if (environmentSettings != null)
        {
            environmentSettings.OnDirectLightChanged += OnDirectLightChanged;
            environmentSettings.OnAmbientLightChanged += OnAmbientLightChanged;
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
