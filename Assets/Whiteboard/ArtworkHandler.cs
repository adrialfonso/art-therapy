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
