using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class GraffitiMarkerStrategy : BaseMarkerStrategy
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

        // Apply circular brush shape, if the pixel is within the radius, paint it following the Graffiti strategy (randomized spray effect)
        for (int j = 0; j < brushSize; j++)
        {
            for (int i = 0; i < brushSize; i++)
            {
                float dx = i - radius;
                float dy = j - radius;
                int index = j * brushSize + i;

                if (dx * dx + dy * dy <= radius * radius)
                {
                    // One percent chance to paint each pixel to create a spray effect
                    if (Random.value > 0.99f)
                    {
                        if (isErasing)
                            newPixels[index] = Color.Lerp(currentPixels[index], originalTexturePixels[index], Random.Range(0.4f, 0.8f));
                        else
                            // Blend the brush color with the current pixel color for a more natural spray effect
                            newPixels[index] = Color.Lerp(currentPixels[index], brushColors[index], Random.Range(0.4f, 0.8f));
                    }
                    else
                    {
                        newPixels[index] = currentPixels[index];
                    }
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
