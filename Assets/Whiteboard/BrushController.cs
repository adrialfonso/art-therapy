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
    
    [Header("Environment Settings Observer")]
    [SerializeField] private EnvironmentSettingsObserver environmentSettings;

    [Header("Skybox Options")]
    [SerializeField] private Material[] skyboxOptions;

    [Header("Ambient Music Options")]
    [SerializeField] private AudioClip[] ambientMusicOptions;
    [SerializeField] private AudioSource ambientSource;

    [Header("3D Mode")]
    [SerializeField] public Transform rightTipTransform;   
    [SerializeField] public Transform leftTipTransform; 
    [SerializeField] public LineRenderer linePrefab;
    
    private LineRenderer currentLine;
    private int index;
    private bool is3DMode = false;

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
        SelectRandomSkybox();
        SelectRandomAmbientMusic();
    }

    private void Update()
    {
        if (is3DMode)
        {
            HandleDrawing3D();
        }
        else
        {
            HandleDrawing(leftState);
            HandleDrawing(rightState);
        }
    }

    // Handle 3D drawing logic using LineRenderer
    private float snapRadius = 0.03f;
    private List<Vector3> existingPoints = new List<Vector3>();

    private void HandleDrawing3D()
    {
        bool isDrawing = leftState.isDrawing || rightState.isDrawing;

        if (isDrawing)
        {
            Transform drawingTip = leftState.isDrawing ? leftTipTransform : rightTipTransform;

            if (currentLine == null)
            {
                index = 0;
                
                // Start a new line
                currentLine = Instantiate(linePrefab);
                currentLine.material.color = brushSettings.BrushColor;

                float baseWidth = brushSettings.BrushSize * 0.0025f;

                AnimationCurve brushCurve = new AnimationCurve(
                    new Keyframe(0f, 0.5f),   
                    new Keyframe(0.5f, 1f),   
                    new Keyframe(1f, 0.5f)    
                );

                currentLine.widthMultiplier = baseWidth;
                currentLine.widthCurve = brushCurve;
                currentLine.positionCount = 1;

                // Connect with existing points if close enough (start point)
                Vector3 startPos = drawingTip.position;
                foreach (var p in existingPoints)
                {
                    if (Vector3.Distance(p, drawingTip.position) <= snapRadius)
                    {
                        startPos = p;
                        break;
                    }
                }

                currentLine.SetPosition(0, startPos);
                existingPoints.Add(startPos);
            }
            else
            {
                float distance = Vector3.Distance(
                    currentLine.GetPosition(index),
                    drawingTip.position
                );

                // Add new point if moved enough
                if (distance > 0.02f)
                {
                    index++;
                    currentLine.positionCount = index + 1;
                    currentLine.SetPosition(index, drawingTip.position);
                    existingPoints.Add(drawingTip.position);
                }
            }
        }
        else
        {
            if (currentLine != null)
            {
                // Connect with existing points if close enough (end point)
                Vector3 lastPos = currentLine.GetPosition(index);
                foreach (var p in existingPoints)
                {
                    if (Vector3.Distance(p, lastPos) <= snapRadius)
                    {
                        currentLine.SetPosition(index, p);
                        break;
                    }
                }
            }

            currentLine = null;
        }
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

    // Toggle between 2D and 3D drawing modes (observer pattern)
    private void Toggle3DMode(bool active)
    {
        is3DMode = active;
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
