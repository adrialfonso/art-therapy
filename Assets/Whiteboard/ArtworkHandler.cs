using System.IO;

// Abstract class to handle artwork operations
public abstract class ArtworkHandler
{
    protected BrushController controller;

    public ArtworkHandler(BrushController controller)
    {
        this.controller = controller;
    }

    public abstract void HandleDrawing();
    public abstract void Undo();
    
    // Toggle between eraser and brush modes
    public virtual void ToggleEraseMode()
    {
        controller.isErasing = !controller.isErasing;
        controller.messageLogger.Log(controller.isErasing ? "Eraser Mode Activated" : "Brush Mode Activated");
    }

    public abstract void SaveArtwork();
    public abstract void LoadArtwork();
    public abstract void ClearArtwork();
    public abstract string[] GetArtworks();
    
    // Create a new blank artwork
    public virtual void NewArtwork()
    {
        ClearArtwork();
        controller.currentArtworkIndex = -1;
        controller.messageLogger.Log("New Artwork Created");
    }

    // Delete the current artwork
    public virtual void DeleteArtwork()
    {
        string filePath = controller.savedWhiteboardArtworks[controller.currentArtworkIndex];
        File.Delete(filePath);
        controller.currentArtworkIndex = -1;
        ClearArtwork();
        controller.savedWhiteboardArtworks = GetArtworks();
        controller.messageLogger.Log("Deleted Artwork: " + Path.GetFileName(filePath));
    }
}
