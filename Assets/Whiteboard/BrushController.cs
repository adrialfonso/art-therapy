using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.IO;

// Abstract class to handle artwork for 2D and 3D modes
public abstract class ArtworkHandler
{
    protected BrushController controller;

    public ArtworkHandler(BrushController controller)
    {
        this.controller = controller;
    }

    public abstract void HandleDrawing();
    public abstract void Undo();
    public abstract void SaveArtwork();
    public abstract void LoadArtwork();
}

// Implementation for 2D whiteboard drawing
public class ArtworkHandler2D : ArtworkHandler
{
    public ArtworkHandler2D(BrushController controller) : base(controller) { }

    public override void HandleDrawing()
    {
        bool isDrawing = controller.isDrawingLeft || controller.isDrawingRight;

        // If not drawing, reset state and return
        if (!isDrawing)
        {
            controller.touchedLastFrame = false;
            controller.savedUndoState = false;
            return;
        }

        XRRayInteractor activeRay = controller.isDrawingLeft ? controller.leftRay : controller.rightRay;

        // Perform raycast and handle drawing
        if (activeRay.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != controller.whiteboard)
            {
                controller.whiteboard = hitBoard;
                controller.savedUndoState = false;
            }

            Vector2 touchPos = new Vector2(hit.textureCoord.x * controller.whiteboard.textureSize.x,
                                           hit.textureCoord.y * controller.whiteboard.textureSize.y);

            // Save undo state if not already saved
            if (!controller.savedUndoState)
            {
                controller.whiteboard.SaveUndoState();
                controller.savedUndoState = true;
            }

            // Draw using the current strategy
            if (controller.touchedLastFrame)
            {
                controller.currentStrategy.Draw(controller.whiteboard, touchPos, controller.lastTouchPos,
                                                controller.brushSettings.BrushSize, controller.brushColors, controller.isErasing);
            }

            controller.lastTouchPos = touchPos;
            controller.touchedLastFrame = true;
        }
        else
        {
            controller.touchedLastFrame = false;
        }
    }

    public override void Undo()
    {
        if (controller.whiteboard != null)
            controller.whiteboard.Undo();
    }

    public override void SaveArtwork()
    {
        Texture2D texture = controller.whiteboard.GetTexture();
        byte[] bytes = texture.EncodeToPNG();
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName;
        bool isOverwrite = controller.currentArtworkIndex >= 0 && controller.savedWhiteboardArtworks != null && controller.currentArtworkIndex < controller.savedWhiteboardArtworks.Length;

        // If already loaded, overwrite the file
        if (isOverwrite)
        {
            fileName = Path.GetFileName(controller.savedWhiteboardArtworks[controller.currentArtworkIndex]);
        }
        else
        {
            fileName = $"Artwork_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        }

        File.WriteAllBytes(Path.Combine(folderPath, fileName), bytes);

        // Update the list of artworks
        controller.savedWhiteboardArtworks = Directory.GetFiles(folderPath, "*.png");

        // If it's a new file, update the index to the end
        if (!isOverwrite)
            controller.currentArtworkIndex = controller.savedWhiteboardArtworks.Length - 1;
    }

    public override void LoadArtwork()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");

        controller.savedWhiteboardArtworks = Directory.GetFiles(folderPath, "*.png").OrderBy(f => File.GetCreationTime(f)).ToArray();
        if (controller.savedWhiteboardArtworks.Length == 0) return;

        controller.currentArtworkIndex = (controller.currentArtworkIndex + 1) % controller.savedWhiteboardArtworks.Length;

        byte[] fileData = File.ReadAllBytes(controller.savedWhiteboardArtworks[controller.currentArtworkIndex]);
        Texture2D loadedTexture = new Texture2D(2, 2);
        loadedTexture.LoadImage(fileData);

        controller.whiteboard.SetTexture(loadedTexture);
    }
}

// Implementation for 3D drawing
public class ArtworkHandler3D : ArtworkHandler
{
    public ArtworkHandler3D(BrushController controller) : base(controller) { }

    public override void HandleDrawing()
    {
        bool isDrawing = controller.isDrawingLeft || controller.isDrawingRight;

        if (isDrawing)
        {
            Transform drawingTip = controller.isDrawingLeft ? controller.leftTipTransform : controller.rightTipTransform;

            if (controller.currentLine == null)
            {
                controller.index = 0;

                // Start a new line
                controller.currentLine = Object.Instantiate(controller.linePrefab);
                controller.currentLine.material.color = controller.brushSettings.BrushColor;

                float baseWidth = controller.brushSettings.BrushSize * 0.0025f;
                AnimationCurve brushCurve = new AnimationCurve(
                    new Keyframe(0f, 0.5f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 0.5f)
                );

                controller.currentLine.widthMultiplier = baseWidth;
                controller.currentLine.widthCurve = brushCurve;
                controller.currentLine.positionCount = 1;

                // Connect with existing points if close enough (start point)
                Vector3 startPos = drawingTip.position;
                foreach (var p in controller.existingPoints)
                {
                    if (Vector3.Distance(p, drawingTip.position) <= controller.snapRadius)
                    {
                        startPos = p;
                        break;
                    }
                }

                controller.currentLine.SetPosition(0, startPos);
                controller.existingPoints.Add(startPos);

                // Update lineHistory
                controller.lineHistory.Add(controller.currentLine);
            }
            else
            {
                float distance = Vector3.Distance(controller.currentLine.GetPosition(controller.index), drawingTip.position);

                // Add new point if moved enough
                if (distance > 0.02f)
                {
                    controller.index++;
                    controller.currentLine.positionCount = controller.index + 1;
                    controller.currentLine.SetPosition(controller.index, drawingTip.position);
                    controller.existingPoints.Add(drawingTip.position);
                }
            }
        }
        else
        {
            if (controller.currentLine != null)
            {
                // Connect with existing points if close enough (end point)
                Vector3 lastPos = controller.currentLine.GetPosition(controller.index);
                foreach (var p in controller.existingPoints)
                {
                    if (Vector3.Distance(p, lastPos) <= controller.snapRadius)
                    {
                        controller.currentLine.SetPosition(controller.index, p);
                        break;
                    }
                }
            }

            controller.currentLine = null;
        }
    }

    public override void Undo()
    {
        if (controller.lineHistory.Count > 0)
        {
            // Destroy the last 3D line
            LineRenderer lastLine = controller.lineHistory[controller.lineHistory.Count - 1];
            controller.lineHistory.RemoveAt(controller.lineHistory.Count - 1);
            Object.Destroy(lastLine.gameObject);
        }
    }

    public override void SaveArtwork()
    {
        LineCollection collection = new LineCollection();

        // Serialize each line's data
        foreach (var line in controller.lineHistory)
        {
            LineData data = new LineData();
            data.points = new Vector3[line.positionCount];
            line.GetPositions(data.points);
            data.color = line.material.color;
            data.width = line.widthMultiplier;
            data.widthCurve = line.widthCurve;

            collection.lines.Add(data);
        }

        string json = JsonUtility.ToJson(collection, true);

        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName;
        string[] files = Directory.GetFiles(folderPath, "*.json");
        if (controller.currentArtworkIndex >= 0 && files.Length > 0 && controller.currentArtworkIndex < files.Length)
        {
            fileName = Path.GetFileName(files[controller.currentArtworkIndex]);
        }
        else
        {
            fileName = $"Artwork3D_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        }

        File.WriteAllText(Path.Combine(folderPath, fileName), json);
    }

    public override void LoadArtwork()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        string[] files = Directory.GetFiles(folderPath, "*.json");

        if (files.Length == 0) return;

        controller.currentArtworkIndex = (controller.currentArtworkIndex + 1) % files.Length;

        string json = File.ReadAllText(files[controller.currentArtworkIndex]);
        LineCollection collection = JsonUtility.FromJson<LineCollection>(json);

        // Clear current lines
        foreach (var line in controller.lineHistory)
            Object.Destroy(line.gameObject);

        controller.lineHistory.Clear();
        controller.existingPoints.Clear();

        // Reconstruct lines from loaded data
        foreach (var data in collection.lines)
        {
            LineRenderer newLine = Object.Instantiate(controller.linePrefab);
            newLine.positionCount = data.points.Length;
            newLine.SetPositions(data.points);
            newLine.material.color = data.color;
            newLine.widthMultiplier = data.width;
            newLine.widthCurve = data.widthCurve;

            controller.lineHistory.Add(newLine);
            controller.existingPoints.AddRange(data.points);
        }
    }
}

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

    // 3D references
    public LineRenderer currentLine;
    public List<Vector3> existingPoints = new List<Vector3>();
    public List<LineRenderer> lineHistory = new List<LineRenderer>();
    public int index;
    public float snapRadius = 0.03f;

    // 2D references
    public Whiteboard whiteboard;
    public Color[] brushColors;
    public List<IMarkerStrategy> strategies;
    public IMarkerStrategy currentStrategy;
    public bool isErasing;
    public bool touchedLastFrame = false;
    public bool savedUndoState = false;
    public Vector2 lastTouchPos;

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
