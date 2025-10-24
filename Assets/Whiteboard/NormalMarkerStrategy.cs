using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class NormalMarkerStrategy : BaseMarkerStrategy
{
    protected override void DrawAtPosition(Whiteboard whiteboard, Vector2 pos, int brushSize, Color[] brushColors, bool isErasing)
    {    
        // Ensure we can paint at the given position
        if (!CanPaint(whiteboard, pos, brushSize)) { return; }

        // Center the brush on the position
        int x = (int)(pos.x - brushSize / 2);
        int y = (int)(pos.y - brushSize / 2);

        int radius = brushSize / 2;
        Color[] currentPixels = whiteboard.texture.GetPixels(x, y, brushSize, brushSize);
        Color[] originalTexturePixels = whiteboard.originalTexture.GetPixels(x, y, brushSize, brushSize);
        Color[] newPixels = new Color[brushSize * brushSize];

        // Apply circular brush shape, if the pixel is within the radius, paint it following the Normal strategy (solid color from brushColors)
        for (int j = 0; j < brushSize; j++)
        {
            for (int i = 0; i < brushSize; i++)
            {
                float dx = i - radius;
                float dy = j - radius;
                int index = j * brushSize + i;

                if (dx * dx + dy * dy <= radius * radius)
                {   
                    if (isErasing)
                        newPixels[index] = originalTexturePixels[index];
                    else
                        newPixels[index] = brushColors[index];
                }
                else
                {
                    newPixels[index] = currentPixels[index];
                }
            }
        }

        // Apply the new pixels to the whiteboard texture
        whiteboard.texture.SetPixels(x, y, brushSize, brushSize, newPixels);
    }
}
