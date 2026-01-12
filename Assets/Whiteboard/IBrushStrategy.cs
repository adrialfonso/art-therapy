using UnityEngine;

public interface IBrushStrategy
{
    // Strategy method to draw on the whiteboard, overridden by concrete strategies
    void Draw(Whiteboard whiteboard, Vector2 currentPos, Vector2 lastPos, int brushSize, Color[] brushColors, bool isErasing);
}
