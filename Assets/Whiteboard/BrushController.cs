using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.IO;

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

    private Light directionalLight;
    
    // 3D references
    private LineRenderer currentLine;
    private List<Vector3> existingPoints = new List<Vector3>();
    private List<LineRenderer> lineHistory = new List<LineRenderer>();
    private int index;
    private bool is3DMode = false;
    private float snapRadius = 0.03f;

    // 2D references
    private Whiteboard whiteboard;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private IMarkerStrategy currentStrategy;
    private bool isErasing;

    private int currentArtworkIndex = -1; 
    private string[] savedWhiteboardArtworks; 

    private bool isDrawingLeft = false;
    private bool isDrawingRight = false;

    private bool touchedLastFrame = false;
    private bool savedUndoState = false;
    private Vector2 lastTouchPos;

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
    }

    private void Update()
    {
        if (is3DMode)
        {
            HandleDrawing3D();
        }
        else
        {
            HandleDrawing();
        }
    }

    // Handle 3D drawing logic using LineRenderer
    private void HandleDrawing3D()
    {
        bool isDrawing = isDrawingLeft || isDrawingRight;

        if (isDrawing)
        {
            Transform drawingTip = isDrawingLeft ? leftTipTransform : rightTipTransform;

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

                // Update lineHistory
                lineHistory.Add(currentLine);
            }
            else
            {
                float distance = Vector3.Distance(currentLine.GetPosition(index), drawingTip.position);

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

    // Handle whiteboard drawing logic (Raycasting)
    private void HandleDrawing()
    {
        bool isDrawing = isDrawingLeft || isDrawingRight;

        // If not drawing, reset state and return
        if (!isDrawing)
        {
            touchedLastFrame = false;
            savedUndoState = false;
            return;
        }

        XRRayInteractor activeRay = isDrawingLeft ? leftRay : rightRay;

        // Perform raycast and handle drawing
        if (activeRay.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            // Get or cache the whiteboard reference
            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != whiteboard)
            {
                whiteboard = hitBoard;
                savedUndoState = false;
            }

            // Calculate touch position on the whiteboard texture
            Vector2 touchPos = new Vector2(hit.textureCoord.x * whiteboard.textureSize.x,
                                           hit.textureCoord.y * whiteboard.textureSize.y);

            // Save undo state if not already saved for this drawing session
            if (!savedUndoState)
            {
                whiteboard.SaveUndoState();
                savedUndoState = true;
            }

            // Draw using the current strategy
            if (touchedLastFrame)
            {
                currentStrategy.Draw(whiteboard, touchPos, lastTouchPos, brushSettings.BrushSize, brushColors, isErasing);
            }

            lastTouchPos = touchPos;
            touchedLastFrame = true;
        }
        else
        {
            touchedLastFrame = false;
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

    // Perform undo (observer pattern)
    private void Undo()
    {
        if (is3DMode)
        {
            if (lineHistory.Count > 0)
            {
                // Destroy the last 3D line
                LineRenderer lastLine = lineHistory[lineHistory.Count - 1];
                lineHistory.RemoveAt(lineHistory.Count - 1);
                Destroy(lastLine.gameObject);
            }
        }
        else
        {
            if (whiteboard != null)
                whiteboard.Undo();
        }
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

    // Save current artwork to persistent storage (observer pattern)
    private void SaveArtwork()
    {
        if (!is3DMode)
        {
            Texture2D texture = whiteboard.GetTexture();
            byte[] bytes = texture.EncodeToPNG();
            string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fileName;
            bool isOverwrite = currentArtworkIndex >= 0 && savedWhiteboardArtworks != null && currentArtworkIndex < savedWhiteboardArtworks.Length;

            // If already loaded, overwrite the file
            if (isOverwrite)
            {
                fileName = Path.GetFileName(savedWhiteboardArtworks[currentArtworkIndex]);
            }
            else
            {
                fileName = $"Artwork_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
            }

            File.WriteAllBytes(Path.Combine(folderPath, fileName), bytes);

            // Update the list of artworks
            savedWhiteboardArtworks = Directory.GetFiles(folderPath, "*.png");

            // If it's a new file, update the index to the end
            if (!isOverwrite)
            {
                currentArtworkIndex = savedWhiteboardArtworks.Length - 1;
            }
        }
        else
        {
            LineCollection collection = new LineCollection();

            // Serialize each line's data
            foreach (var line in lineHistory)
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

            // If already loaded, overwrite the file
            string[] files = Directory.GetFiles(folderPath, "*.json");
            if (currentArtworkIndex >= 0 && files.Length > 0 && currentArtworkIndex < files.Length)
            {
                fileName = Path.GetFileName(files[currentArtworkIndex]);
            }
            else
            {
                fileName = $"Artwork3D_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
            }

            File.WriteAllText(Path.Combine(folderPath, fileName), json);
        }
    }

    // Load artworks from storage (observer pattern)
    private void LoadArtwork()
    {
        if (!is3DMode)
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");

            savedWhiteboardArtworks = Directory.GetFiles(folderPath, "*.png").OrderBy(f => File.GetCreationTime(f)).ToArray();
            if (savedWhiteboardArtworks.Length == 0) return;

            currentArtworkIndex = (currentArtworkIndex + 1) % savedWhiteboardArtworks.Length;

            byte[] fileData = File.ReadAllBytes(savedWhiteboardArtworks[currentArtworkIndex]);
            Texture2D loadedTexture = new Texture2D(2, 2);
            loadedTexture.LoadImage(fileData);

            whiteboard.SetTexture(loadedTexture);
        }
        else
        {
            string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
            string[] files = Directory.GetFiles(folderPath, "*.json");

            if (files.Length == 0) return;

            currentArtworkIndex = (currentArtworkIndex + 1) % files.Length;

            string json = File.ReadAllText(files[currentArtworkIndex]);
            LineCollection collection = JsonUtility.FromJson<LineCollection>(json);

            // Clear current lines
            foreach (var line in lineHistory)
                Destroy(line.gameObject);

            lineHistory.Clear();
            existingPoints.Clear();

            // Reconstruct lines from loaded data
            foreach (var data in collection.lines)
            {
                LineRenderer newLine = Instantiate(linePrefab);
                newLine.positionCount = data.points.Length;
                newLine.SetPositions(data.points);
                newLine.material.color = data.color;
                newLine.widthMultiplier = data.width;
                newLine.widthCurve = data.widthCurve;

                lineHistory.Add(newLine);
                existingPoints.AddRange(data.points);
            }
        }
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
