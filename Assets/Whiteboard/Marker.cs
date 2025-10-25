using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;

public class Marker : MonoBehaviour
{
    [Header("Brush Settings")]
    [SerializeField] private XRRayInteractor rayInteractor;
    [SerializeField] private int brushSize = 50;
    [SerializeField] private Color brushColor = Color.red;

    // XRI Default Input Actions used for marker interaction
    [Header("XRI Marker Actions")]
    [SerializeField] private InputActionProperty CycleStrategyAction;   
    [SerializeField] private InputActionProperty DrawAction; 
    [SerializeField] private InputActionProperty EraseAction;
    [SerializeField] private InputActionProperty UndoAction;

    private Whiteboard whiteboard;
    private Vector2 lastTouchPos;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private int currentIndex = 0;
    private IMarkerStrategy currentStrategy;
    private bool isErasing, savedUndoState, touchedLastFrame = false;

    void Start()
    {
        if (rayInteractor == null)
            rayInteractor = GetComponent<XRRayInteractor>();

        brushColors = Enumerable.Repeat(brushColor, brushSize * brushSize).ToArray();

        // Initialize marker strategies
        strategies = new List<IMarkerStrategy>
        {
            new NormalMarkerStrategy(),
            new GraffitiMarkerStrategy(),
            new WatercolorMarkerStrategy()
        };

        currentStrategy = strategies[currentIndex];

        if (CycleStrategyAction.action != null)
            CycleStrategyAction.action.performed += _ => CycleStrategy();
        
        if (EraseAction.action != null)
            EraseAction.action.performed += _ => ToggleEraseMode();

        if (UndoAction.action != null)
            UndoAction.action.performed += _ => Undo();
    }

    void Update()
    {
        bool isDrawing = DrawAction.action?.ReadValue<float>() > 0.1f;

        if (isDrawing)
            DrawWithRayInteractor();
        else
        {
            touchedLastFrame = false;
            savedUndoState = false; 
        }
    }

    // Handle drawing on the whiteboard using the ray interactor (both XR Controllers)
    private void DrawWithRayInteractor()
    {
        if (rayInteractor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (hit.transform.CompareTag("Whiteboard"))
            {
                if (whiteboard == null)
                    whiteboard = hit.transform.GetComponent<Whiteboard>();

                // Convert hit texture coordinates to whiteboard texture space
                Vector2 touchPos = new Vector2(hit.textureCoord.x * whiteboard.textureSize.x, hit.textureCoord.y * whiteboard.textureSize.y);

                // Save undo state only once per drawing session
                if (!savedUndoState)
                {
                    whiteboard.SaveUndoState();
                    savedUndoState = true;
                }

                // Draw using the current strategy
                if (touchedLastFrame)
                    currentStrategy.Draw(whiteboard, touchPos, lastTouchPos, brushSize, brushColors, isErasing);

                lastTouchPos = touchPos;
                touchedLastFrame = true;
                return;
            }
        }

        touchedLastFrame = false;
    }

    // Cycle through available marker strategies
    private void CycleStrategy()
    {
        currentIndex = (currentIndex + 1) % strategies.Count;
        currentStrategy = strategies[currentIndex];
    }

    private void ToggleEraseMode()
    {
        isErasing = !isErasing;
    } 

    // Undo the last stroke
    public void Undo()
    {
        if (whiteboard != null)
        {
            whiteboard.Undo();
        }
    }

    public void SetStrategy(int index)
    {
        if (index >= 0 && index < strategies.Count)
        {
            currentIndex = index;
            currentStrategy = strategies[currentIndex];
        }
    }
}
