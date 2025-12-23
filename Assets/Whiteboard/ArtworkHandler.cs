using UnityEngine;
using System.IO;

// Abstract class to handle artwork operations
public abstract class ArtworkHandler
{
    protected BrushContext context;

    public ArtworkHandler(BrushContext context)
    {
        this.context = context;
    }

    public abstract void HandleDrawing();
    public abstract void Undo();
    
    // Toggle between eraser and brush modes
    public virtual void ToggleEraseMode()
    {
        context.isErasing = !context.isErasing;
        context.messageLogger.Log(context.languageSettings.Translate(context.isErasing ? "Eraser Mode Activated" : "Brush Mode Activated"));
    }

    public abstract void SaveArtwork();
    public abstract void LoadArtwork();
    public abstract void ClearArtwork();
    public abstract string[] GetArtworks();
    
    // Create a new blank artwork
    public virtual void NewArtwork()
    {
        ClearArtwork();
        context.currentArtworkIndex = -1;
        context.messageLogger.Log(context.languageSettings.Translate("New Artwork Created"));
    }

    // Delete the current artwork
    public virtual void DeleteArtwork()
    {
        string filePath = context.savedWhiteboardArtworks[context.currentArtworkIndex];
        File.Delete(filePath);
        context.currentArtworkIndex = -1;
        ClearArtwork();
        context.savedWhiteboardArtworks = GetArtworks();
        context.messageLogger.Log(context.languageSettings.Translate("Deleted Artwork: " + Path.GetFileName(filePath)));
    }
}
