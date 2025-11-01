using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class BrushController : MonoBehaviour
{
    [Header("XR Ray Interactors")]
    [SerializeField] private XRRayInteractor leftRay;
    [SerializeField] private XRRayInteractor rightRay;

    [Header("Draw Event Managers")]
    [SerializeField] private DrawEventObserver leftController;
    [SerializeField] private DrawEventObserver rightController;

    [Header("Brush Settings Observer")]
    [SerializeField] private BrushSettingsObserver brushSettings;

    private Whiteboard whiteboard;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private IMarkerStrategy currentStrategy;
    private bool isErasing;

    // Internal state per controller
    private class ControllerState
    {
        public XRRayInteractor ray;
        public bool isDrawing;
        public Vector2 lastTouchPos;
        public bool touchedLastFrame;
        public bool savedUndoState;
    }

    private ControllerState leftState;
    private ControllerState rightState;

    private void OnEnable()
    {   
        // Initialize controller states
        leftState = new ControllerState { ray = leftRay };
        rightState = new ControllerState { ray = rightRay };

        // Subscribe to brush settings changes
        if (brushSettings != null)
        {
            brushSettings.OnBrushSizeChanged += OnBrushSizeChanged;
            brushSettings.OnBrushColorChanged += OnBrushColorChanged;
            brushSettings.OnStrategyChanged += OnStrategyChanged;
        }

        if (leftController != null)
        {
            leftController.OnDrawPressed += () => leftState.isDrawing = true;
            leftController.OnDrawReleased += () => leftState.isDrawing = false;
            leftController.OnErasePressed += ToggleEraseMode;
            leftController.OnUndoPressed += Undo;
        }

        if (rightController != null)
        {
            rightController.OnDrawPressed += () => rightState.isDrawing = true;
            rightController.OnDrawReleased += () => rightState.isDrawing = false;
            rightController.OnErasePressed += ToggleEraseMode;
            rightController.OnUndoPressed += Undo;
        }
    }

    private void Start()
    {
        // Initialize brush colors
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();

        // Initialize strategies
        strategies = new List<IMarkerStrategy>
        {
            new NormalMarkerStrategy(),
            new GraffitiMarkerStrategy(),
            new WatercolorMarkerStrategy()
        };

        currentStrategy = strategies[brushSettings.StrategyIndex];
    }

    private void Update()
    {
        HandleDrawing(leftState);
        HandleDrawing(rightState);
    }

    // Handle drawing logic for a given controller state
    private void HandleDrawing(ControllerState state)
    {
        // If not drawing, reset state and return
        if (!state.isDrawing)
        {
            state.touchedLastFrame = false;
            state.savedUndoState = false;
            return;
        }

        // Perform raycast and handle drawing
        if (state.ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (!hit.transform.CompareTag("Whiteboard")) return;

            // Get or cache the whiteboard reference
            Whiteboard hitBoard = hit.transform.GetComponent<Whiteboard>();
            if (hitBoard != whiteboard)
            {
                whiteboard = hitBoard;
                state.savedUndoState = false;
            }

            // Calculate touch position on the whiteboard texture
            Vector2 touchPos = new Vector2(hit.textureCoord.x * whiteboard.textureSize.x, hit.textureCoord.y * whiteboard.textureSize.y);

            // Save undo state if not already saved for this drawing session
            if (!state.savedUndoState)
            {
                whiteboard.SaveUndoState();
                state.savedUndoState = true;
            }

            // Draw using the current strategy
            if (state.touchedLastFrame)
                currentStrategy.Draw(whiteboard, touchPos, state.lastTouchPos, brushSettings.BrushSize, brushColors, isErasing);

            state.lastTouchPos = touchPos;
            state.touchedLastFrame = true;
        }
        else
        {
            state.touchedLastFrame = false;
        }
    }

    // Toggle between drawing and erasing modes (observer pattern)
    private void ToggleEraseMode()
    {
        isErasing = !isErasing;
    }

    // Perform undo on the whiteboard (observer pattern)
    private void Undo()
    {
        if (whiteboard != null)
        {
            whiteboard.Undo();
        }
    }

    // Listener for brush size changes (observer pattern)
    private void OnBrushSizeChanged(int size)
    {
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, size * size).ToArray();
    }

    // Listener for brush color changes (observer pattern)
    private void OnBrushColorChanged(Color color)
    {
        brushColors = Enumerable.Repeat(color, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();
    }

    // Listener for strategy changes (observer pattern)
    private void OnStrategyChanged(int index)
    {
        if (index >= 0 && index < strategies.Count)
            currentStrategy = strategies[index];
    }
}
