using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Whiteboard : MonoBehaviour
{
    public Texture2D texture, originalTexture;
    public Vector2 textureSize = new Vector2(2048, 2048);

    // Stack to hold previous texture states for undo functionality
    private Stack<Texture2D> undoStack = new Stack<Texture2D>();
    private const int maxUndoStates = 5;

    void Start()
    {
        var renderer = GetComponent<Renderer>();
        texture = new Texture2D((int)textureSize.x, (int)textureSize.y);
        originalTexture = new Texture2D((int)textureSize.x, (int)textureSize.y);

        // Assign the new whiteboard texture on start
        renderer.material.mainTexture = texture;
    }

    // Save current texture state to undo stack
    public void SaveUndoState()
    {
        Texture2D savedTexture = new Texture2D((int)textureSize.x, (int)textureSize.y);
        savedTexture.SetPixels(texture.GetPixels());
        savedTexture.Apply();
        undoStack.Push(savedTexture);

        // Keep stack within max limit
        if (undoStack.Count > maxUndoStates)
        {
            // Remove the oldest state (bottom of stack)
            Texture2D[] tempArray = undoStack.ToArray();
            undoStack.Clear();
            for (int i = tempArray.Length - 2; i >= 0; i--)
                undoStack.Push(tempArray[i]);
        }
    }

    // Restore texture from the latest undo state
    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            Texture2D lastState = undoStack.Pop();
            texture.SetPixels(lastState.GetPixels());
            texture.Apply();
        }
    }

    // Get the current whiteboard texture
    public Texture2D GetTexture()
    {
        return texture;
    }

    // Set a new texture to the whiteboard
    public void SetTexture(Texture2D newTexture)
    {
        texture.SetPixels(newTexture.GetPixels());
        texture.Apply();
    }

    // Clear the whiteboard to its original state
    public void ClearTexture()
    {
        texture.SetPixels(originalTexture.GetPixels());
        texture.Apply();
    }
}