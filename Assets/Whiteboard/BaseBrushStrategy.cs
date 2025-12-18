using UnityEngine;

public abstract class BaseBrushStrategy : IBrushStrategy
{
    // Overridden method common to all BaseBrushStrategy implementations
    public void Draw(Whiteboard whiteboard, Vector2 currentPos, Vector2 lastPos, int brushSize, Color[] brushColors, bool isErasing)
    {
        DrawAtPosition(whiteboard, currentPos, brushSize, brushColors, isErasing);
        SmoothStroke(whiteboard, currentPos, lastPos, brushSize, brushColors, isErasing);
        whiteboard.texture.Apply();
    }

    // Abstract method to be implemented by all BaseMarkerStrategy implementations
    protected abstract void DrawAtPosition(Whiteboard whiteboard, Vector2 pos, int brushSize, Color[] brushColors, bool isErasing);

    // Common method to smooth strokes 
    protected void SmoothStroke(Whiteboard whiteboard, Vector2 currentPos, Vector2 lastPos, int brushSize, Color[] brushColors, bool isErasing)
    {
        for (float f = 0.05f; f < 1.0f; f += 0.2f)
        {
            Vector2 lerpPos = Vector2.Lerp(lastPos, currentPos, f);
            DrawAtPosition(whiteboard, lerpPos, brushSize, brushColors, isErasing);
        }
    }

    // Common method to check if painting is possible at the given position
    protected bool CanPaint(Whiteboard whiteboard, Vector2 pos, int brushSize)
    {
        int x = (int)(pos.x - brushSize / 2);
        int y = (int)(pos.y - brushSize / 2);

        return !(x < 0 || y < 0 || x + brushSize >= whiteboard.textureSize.x || y + brushSize >= whiteboard.textureSize.y);
    }
}
