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

    public Texture2D GetTexture()
    {
        return texture;
    }
}
