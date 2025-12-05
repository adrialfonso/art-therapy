using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.IO;

// Implementation for 2D whiteboard drawing
public class ArtworkHandler2D : ArtworkHandler
{
    private bool touchedLastFrame = false;
    private bool savedUndoState = false;
    private Vector2 lastTouchPos;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private IMarkerStrategy currentStrategy;
    private bool isErasing = false;

    public ArtworkHandler2D(BrushController controller) : base(controller)
    {
        InitializeMarkerStrategies();
        OnBrushSizeChanged(controller.brushSettings.BrushSize);
        OnBrushColorChanged(controller.brushSettings.BrushColor);
    }

    public override void HandleDrawing()
    {
        bool isDrawing = controller.isDrawingLeft || controller.isDrawingRight;

        // If not drawing, reset state and return
        if (!isDrawing)
        {
            touchedLastFrame = false;
            savedUndoState = false;
            return;
        }

        // Determine active ray interactor
        XRRayInteractor activeRay = controller.isDrawingLeft ? controller.leftRay : controller.rightRay;

        // Perform raycast and handle drawing
        if (activeRay.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            // Get or update whiteboard reference
            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != controller.whiteboard)
            {
                controller.whiteboard = hitBoard;
                savedUndoState = false;
            }

            Vector2 touchPos = new Vector2(hit.textureCoord.x * controller.whiteboard.textureSize.x, hit.textureCoord.y * controller.whiteboard.textureSize.y);
            // Save undo state if not already saved
            if (!savedUndoState)
            {
                controller.whiteboard.SaveUndoState();
                savedUndoState = true;
            }

            // Draw using the current drawing strategy
            if (touchedLastFrame)
            {
                currentStrategy.Draw(controller.whiteboard, touchPos, lastTouchPos, controller.brushSettings.BrushSize, brushColors, isErasing);
            }

            lastTouchPos = touchPos;
            touchedLastFrame = true;
        }
        else
        {
            touchedLastFrame = false;
        }
    }

    public override void Undo()
    {
        if (controller.whiteboard != null)
            controller.whiteboard.Undo();
    }

    // Save current artwork to persistent data path (2D)
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
            fileName = $"artwork2D_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";
        }

        File.WriteAllBytes(Path.Combine(folderPath, fileName), bytes);

        // Update the list of artworks
        controller.savedWhiteboardArtworks = Directory.GetFiles(folderPath, "*.png");

        // If it's a new file, update the index to the end
        if (!isOverwrite)
            controller.currentArtworkIndex = controller.savedWhiteboardArtworks.Length - 1;

        controller.messageLogger.Log("Artwork Saved: " + fileName);
    }

    // Load artwork from persistent data path (2D)
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
        controller.messageLogger.Log("Artwork Loaded: " + Path.GetFileName(controller.savedWhiteboardArtworks[controller.currentArtworkIndex]));
    }

    // Clear the whiteboard texture (2D)
    public override void ClearArtwork()
    {
        controller.whiteboard.ClearTexture();
    }

    public void ToggleEraseMode()
    {
        isErasing = !isErasing;
        controller.messageLogger.Log(isErasing ? "Eraser Mode Activated" : "Drawing Mode Activated");
    }

    public void OnBrushSizeChanged(int size)
    {
        brushColors = Enumerable.Repeat(controller.brushSettings.BrushColor, size * size).ToArray();
    }

    public void OnBrushColorChanged(Color color)
    {
        brushColors = Enumerable.Repeat(color, controller.brushSettings.BrushSize * controller.brushSettings.BrushSize).ToArray();
    }

    public void OnStrategyChanged(int index)
    {
        if (index >= 0 && index < strategies.Count)
            currentStrategy = strategies[index];
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

        currentStrategy = strategies[controller.brushSettings.StrategyIndex];
    }
}
