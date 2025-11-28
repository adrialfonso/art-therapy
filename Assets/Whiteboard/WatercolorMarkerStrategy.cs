using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class WatercolorMarkerStrategy : BaseMarkerStrategy
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

        // Base blend factor for watercolor effect
        float baseBlend = 0.02f; 

        // Apply circular brush shape, if the pixel is within the radius, paint it following the Watercolor strategy (Blend with existing color)
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
                    {
                        newPixels[index] = Color.Lerp(currentPixels[index], originalTexturePixels[index], baseBlend);
                    }
                    else
                    {
                        Color blended = Color.Lerp(currentPixels[index], brushColors[index], baseBlend);
                        blended = Color.Lerp(blended, currentPixels[index], Random.Range(0f, 0.5f));
                        newPixels[index] = blended;
                    }
                }
                else
                {
                    newPixels[index] = currentPixels[index];
                }
            }
        }

        whiteboard.texture.SetPixels(x, y, brushSize, brushSize, newPixels);
    }
}
