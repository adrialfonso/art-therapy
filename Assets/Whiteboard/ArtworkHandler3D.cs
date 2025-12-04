using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.SceneManagement;
using System.IO;

// Implementation for 3D drawing
public class ArtworkHandler3D : ArtworkHandler
{
    private LineRenderer currentLine;
    private List<Vector3> existingPoints = new List<Vector3>();
    private List<LineRenderer> lineHistory = new List<LineRenderer>();
    private int index;
    private float snapRadius = 0.03f;

    public ArtworkHandler3D(BrushController controller) : base(controller) { }

    public override void HandleDrawing()
    {
        bool isDrawing = controller.isDrawingLeft || controller.isDrawingRight;

        if (isDrawing)
        {
            Transform drawingTip = controller.isDrawingLeft ? controller.leftTipTransform : controller.rightTipTransform;

            if (currentLine == null)
            {
                index = 0;

                // Start a new line
                currentLine = Object.Instantiate(controller.linePrefab);
                currentLine.material.color = controller.brushSettings.BrushColor;

                float baseWidth = controller.brushSettings.BrushSize * 0.0025f;
                AnimationCurve brushCurve = new AnimationCurve(
                    new Keyframe(0f, 0.5f),
                    new Keyframe(0.5f, 1f),
                    new Keyframe(1f, 0.5f)
                );

                currentLine.widthMultiplier = baseWidth;
                currentLine.widthCurve = brushCurve;
                currentLine.positionCount = 1;

                // Connect with existing points if close enough (start point)
                Vector3 startPos = drawingTip.position;
                foreach (var p in existingPoints)
                {
                    if (Vector3.Distance(p, drawingTip.position) <= snapRadius)
                    {
                        startPos = p;
                        break;
                    }
                }

                currentLine.SetPosition(0, startPos);
                existingPoints.Add(startPos);

                // Update lineHistory
                lineHistory.Add(currentLine);
            }
            else
            {
                float distance = Vector3.Distance(currentLine.GetPosition(index), drawingTip.position);

                // Add new point if moved enough
                if (distance > 0.02f)
                {
                    index++;
                    currentLine.positionCount = index + 1;
                    currentLine.SetPosition(index, drawingTip.position);
                    existingPoints.Add(drawingTip.position);
                }
            }
        }
        else
        {
            if (currentLine != null)
            {
                // Connect with existing points if close enough (end point)
                Vector3 lastPos = currentLine.GetPosition(index);
                foreach (var p in existingPoints)
                {
                    if (Vector3.Distance(p, lastPos) <= snapRadius)
                    {
                        currentLine.SetPosition(index, p);
                        break;
                    }
                }
            }

            currentLine = null;
        }
    }

    public override void Undo()
    {
        if (lineHistory.Count > 0)
        {
            // Destroy the last 3D line
            LineRenderer lastLine = lineHistory[lineHistory.Count - 1];
            lineHistory.RemoveAt(lineHistory.Count - 1);
            Object.Destroy(lastLine.gameObject);
        }
    }

    // Save current artwork to persistent data path (3D)
    public override void SaveArtwork()
    {
        LineCollection collection = new LineCollection();

        // Serialize each line's data
        foreach (var line in lineHistory)
        {
            LineData data = new LineData();
            data.points = new Vector3[line.positionCount];
            line.GetPositions(data.points);
            data.color = line.material.color;
            data.width = line.widthMultiplier;
            data.widthCurve = line.widthCurve;

            collection.lines.Add(data);
        }

        string json = JsonUtility.ToJson(collection, true);

        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName;
        string[] files = Directory.GetFiles(folderPath, "*.json");
        if (controller.currentArtworkIndex >= 0 && files.Length > 0 && controller.currentArtworkIndex < files.Length)
        {
            fileName = Path.GetFileName(files[controller.currentArtworkIndex]);
        }
        else
        {
            fileName = $"Artwork3D_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        }

        File.WriteAllText(Path.Combine(folderPath, fileName), json);
    }

    // Load artwork from persistent data path (3D)
    public override void LoadArtwork()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        string[] files = Directory.GetFiles(folderPath, "*.json");

        if (files.Length == 0) return;

        controller.currentArtworkIndex = (controller.currentArtworkIndex + 1) % files.Length;

        string json = File.ReadAllText(files[controller.currentArtworkIndex]);
        LineCollection collection = JsonUtility.FromJson<LineCollection>(json);

        ClearArtwork();

        // Reconstruct lines from loaded data
        foreach (var data in collection.lines)
        {
            LineRenderer newLine = Object.Instantiate(controller.linePrefab);
            newLine.positionCount = data.points.Length;
            newLine.SetPositions(data.points);
            newLine.material.color = data.color;
            newLine.widthMultiplier = data.width;
            newLine.widthCurve = data.widthCurve;

            lineHistory.Add(newLine);
            existingPoints.AddRange(data.points);
        }
    }

    // Clear all 3D lines (3D)
    public override void ClearArtwork()
    {
        foreach (var line in lineHistory)
            Object.Destroy(line.gameObject);

        lineHistory.Clear();
        existingPoints.Clear();
    }
}
