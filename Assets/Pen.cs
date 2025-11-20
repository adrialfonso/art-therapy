using UnityEngine;
using UnityEngine.InputSystem;

public class Pen : MonoBehaviour
{
    public Transform controllerTransform;    
    public Material drawingMaterial;
    public float lineWidth = 0.01f;
    public Color lineColor = Color.black;

    public InputActionProperty drawAction;     

    private LineRenderer currentLine;
    private int index;

    void Update()
    {
        bool isDrawing = drawAction.action.ReadValue<float>() > 0.5f;

        if (isDrawing)
        {
            Draw();
        }
        else
        {
            currentLine = null;
        }
    }

    void Draw()
    {
        if (currentLine == null)
        {
            index = 0;

            currentLine = new GameObject("Line").AddComponent<LineRenderer>();
            currentLine.material = drawingMaterial;
            currentLine.startWidth = currentLine.endWidth = lineWidth;
            currentLine.startColor = currentLine.endColor = lineColor;

            currentLine.positionCount = 1;
            currentLine.SetPosition(0, controllerTransform.position);
        }
        else
        {
            float distance = Vector3.Distance(
                currentLine.GetPosition(index),
                controllerTransform.position
            );

            if (distance > 0.01f)
            {
                index++;
                currentLine.positionCount = index + 1;
                currentLine.SetPosition(index, controllerTransform.position);
            }
        }
    }
}
