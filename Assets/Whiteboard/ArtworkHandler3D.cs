using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

// Implementation for 3D drawing
public class ArtworkHandler3D : ArtworkHandler
{
    private LineRenderer currentLine;
    private List<Vector3> existingPoints = new List<Vector3>();
    private List<LineRenderer> lineHistory = new List<LineRenderer>();
    private int index;
    private float snapRadius = 0.015f;

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

                float baseWidth = controller.brushSettings.BrushSize * 0.002f;
                float curve = controller.brushSettings.BrushCurve;
                
                AnimationCurve brushCurve = new AnimationCurve(
                    new Keyframe(0f, curve),  
                    new Keyframe(0.5f, 1f), 
                    new Keyframe(1f, curve)    
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
                if (distance > 0.01f)
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

        // Create directory if it doesn't exist
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string[] files = GetArtworks();
        string fileName;

        bool isOverwrite = controller.currentArtworkIndex >= 0 && files.Length > 0 && controller.currentArtworkIndex < files.Length;

        if (isOverwrite)
            fileName = Path.GetFileName(files[controller.currentArtworkIndex]);
        else
            fileName = $"artwork3D_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";

        File.WriteAllText(Path.Combine(folderPath, fileName), json);

        // Refresh the saved artwork list
        controller.savedWhiteboardArtworks = GetArtworks();

        // Update current index if new artwork
        if (!isOverwrite)
            controller.currentArtworkIndex = controller.savedWhiteboardArtworks.Length - 1;

        controller.messageLogger.Log("Artwork Saved: " + fileName);
    }

    // Load artwork from persistent data path (3D)
    public override void LoadArtwork()
    {
        controller.savedWhiteboardArtworks = GetArtworks();
        if (controller.savedWhiteboardArtworks.Length == 0) return;

        controller.currentArtworkIndex = (controller.currentArtworkIndex + 1) % controller.savedWhiteboardArtworks.Length;

        string json = File.ReadAllText(controller.savedWhiteboardArtworks[controller.currentArtworkIndex]);
        LineCollection collection = JsonUtility.FromJson<LineCollection>(json);

        ClearArtwork();
        controller.StartCoroutine(LoadArtworkWithDelay(collection, 0.02f));
    }

    // Clear all 3D lines
    public override void ClearArtwork()
    {
        foreach (var line in lineHistory)
            Object.Destroy(line.gameObject);

        lineHistory.Clear();
        existingPoints.Clear();
    }

    // Refresh the list of saved artworks (3D)
    public override string[] GetArtworks()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "artworks", "3D");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        return Directory.GetFiles(folderPath, "*.json").OrderBy(f => File.GetCreationTime(f)).ToArray();
    }

    // Load artwork with delay between lines for visual effect
    private IEnumerator LoadArtworkWithDelay(LineCollection collection, float delay)
    {
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

            // Pause between line loads
            yield return new WaitForSeconds(delay);
        }

        controller.messageLogger.Log("Artwork Loaded: " + Path.GetFileName(controller.savedWhiteboardArtworks[controller.currentArtworkIndex]));
    }
}
