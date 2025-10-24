using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMarkerStrategy
{
    // Strategy method to draw on the whiteboard, overridden by concrete strategies
    void Draw(Whiteboard whiteboard, Vector2 currentPos, Vector2 lastPos, int brushSize, Color[] brushColors, bool isErasing);
}
