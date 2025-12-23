using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.IO;

// Implementation for 2D whiteboard drawing
public class ArtworkHandler2D : ArtworkHandler
{
    private bool touchedLastFrame = false;
    private bool savedUndoState = false;
    private Vector2 lastTouchPos;
    private Color[] brushColors;
    private List<IBrushStrategy> strategies;
    private IBrushStrategy currentStrategy;

    public ArtworkHandler2D(BrushContext context) : base(context)
    {
        InitializeBrushStrategies();
        OnBrushSizeChanged(context.brushSettings.BrushSize);
        OnBrushColorChanged(context.brushSettings.BrushColor);
    }

    // Check if the first hit of the ray interactor is on UI
    private bool FirstHitIsUI(XRRayInteractor rayInteractor)
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            return hit.collider.gameObject.layer == LayerMask.NameToLayer("UI");
        }

        return false;
    }

    public override void HandleDrawing()
    {
        bool isDrawing = context.isDrawingLeft || context.isDrawingRight;

        // If not drawing, reset state and return
        if (!isDrawing)
        {
            touchedLastFrame = false;
            savedUndoState = false;
            return;
        }

        // Determine active ray interactor
        XRRayInteractor activeRay = context.isDrawingLeft ? context.leftRay : context.rightRay;

        if (FirstHitIsUI(activeRay))
        {
            touchedLastFrame = false;
            savedUndoState = false;
            return;
        }

        // Perform raycast and handle drawing
        if (activeRay.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != context.whiteboard)
            {
                context.whiteboard = hitBoard;
                savedUndoState = false;
            }

            Vector2 touchPos = new Vector2(hit.textureCoord.x * context.whiteboard.textureSize.x, hit.textureCoord.y * context.whiteboard.textureSize.y);
            
            // Save undo state if not already saved
            if (!savedUndoState)
            {
                context.whiteboard.SaveUndoState();
                savedUndoState = true;
            }

            // Draw using the current drawing strategy
            if (touchedLastFrame)
            {
                currentStrategy.Draw(context.whiteboard, touchPos, lastTouchPos, context.brushSettings.BrushSize, brushColors, context.isErasing);
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
        if (context.whiteboard != null)
            context.whiteboard.Undo();
    }

    // Save current artwork to persistent data path (2D)
    public override void SaveArtwork()
    {
        Texture2D texture = context.whiteboard.GetTexture();
        byte[] bytes = texture.EncodeToPNG();
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string[] files = GetArtworks();
        string fileName;
        bool isOverwrite = context.currentArtworkIndex >= 0 && files.Length > 0 && context.currentArtworkIndex < files.Length;

        if (isOverwrite)
            fileName = Path.GetFileName(files[context.currentArtworkIndex]);
        else
            fileName = $"artwork2D_{System.DateTime.Now:yyyyMMdd_HHmmss}.png";

        File.WriteAllBytes(Path.Combine(folderPath, fileName), bytes);

        // Refresh the saved artwork list
        context.savedWhiteboardArtworks = GetArtworks();

        // Update current index if new artwork
        if (!isOverwrite)
            context.currentArtworkIndex = context.savedWhiteboardArtworks.Length - 1;

        context.messageLogger.Log(context.languageSettings.Translate("Artwork Saved: " + fileName));
    }

    // Load artwork from persistent data path (2D)
    public override void LoadArtwork()
    {
        context.savedWhiteboardArtworks = GetArtworks();
        if (context.savedWhiteboardArtworks.Length == 0) return;

        context.currentArtworkIndex = (context.currentArtworkIndex + 1) % context.savedWhiteboardArtworks.Length;

        byte[] fileData = File.ReadAllBytes(context.savedWhiteboardArtworks[context.currentArtworkIndex]);
        Texture2D loadedTexture = new Texture2D(2, 2);
        loadedTexture.LoadImage(fileData);

        context.whiteboard.SetTexture(loadedTexture);
        context.messageLogger.Log(context.languageSettings.Translate("Artwork Loaded: " + Path.GetFileName(context.savedWhiteboardArtworks[context.currentArtworkIndex])));
    }

    // Clear the whiteboard texture (2D)
    public override void ClearArtwork()
    {
        context.whiteboard.ClearTexture();
    }

    // Refresh the list of saved artworks (2D)
    public override string[] GetArtworks()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "2D");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        return Directory.GetFiles(folderPath, "*.png").OrderBy(f => File.GetCreationTime(f)).ToArray();
    }

    public void OnBrushSizeChanged(int size)
    {
        brushColors = Enumerable.Repeat(context.brushSettings.BrushColor, size * size).ToArray();
    }

    public void OnBrushColorChanged(Color color)
    {
        brushColors = Enumerable.Repeat(color, context.brushSettings.BrushSize * context.brushSettings.BrushSize).ToArray();
    }

    public void OnStrategyChanged(int index)
    {
        if (index >= 0 && index < strategies.Count)
            currentStrategy = strategies[index];
    }

    // Initialize available brush strategies
    private void InitializeBrushStrategies()
    {
        strategies = new List<IBrushStrategy>
        {
            new NormalBrushStrategy(),
            new GraffitiBrushStrategy(),
            new WatercolorBrushStrategy()
        };

        currentStrategy = strategies[context.brushSettings.StrategyIndex];
    }
}
