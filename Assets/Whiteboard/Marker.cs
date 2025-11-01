using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class Marker : MonoBehaviour
{
    [SerializeField] private XRRayInteractor rayInteractor;

    // Reference to BrushSettings for observing brush property changes
    [SerializeField] private BrushSettings brushSettings;

    private Whiteboard whiteboard;
    private Vector2 lastTouchPos;
    private Color[] brushColors;
    private List<IMarkerStrategy> strategies;
    private IMarkerStrategy currentStrategy;
    private bool isErasing, savedUndoState, touchedLastFrame = false;

    // XRI Default Input Actions used for marker interaction
    [Header("XRI Marker Actions")] 
    [SerializeField] private InputActionProperty DrawAction; 
    [SerializeField] private InputActionProperty EraseAction;
    [SerializeField] private InputActionProperty UndoAction;

    private void OnEnable()
    {
        if (brushSettings != null)
        {
            brushSettings.OnBrushSizeChanged += OnBrushSizeChanged;
            brushSettings.OnBrushColorChanged += OnBrushColorChanged;
            brushSettings.OnStrategyChanged += OnStrategyChanged;
        }
    }

    private void OnDisable()
    {
        if (brushSettings != null)
        {
            brushSettings.OnBrushSizeChanged -= OnBrushSizeChanged;
            brushSettings.OnBrushColorChanged -= OnBrushColorChanged;
            brushSettings.OnStrategyChanged -= OnStrategyChanged;
        }
    }

    private void Start()
    {
        if (rayInteractor == null)
            rayInteractor = GetComponent<XRRayInteractor>();

        brushColors = Enumerable.Repeat(brushSettings.BrushColor, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();

        // Initialize marker strategies
        strategies = new List<IMarkerStrategy>
        {
            new NormalMarkerStrategy(),
            new GraffitiMarkerStrategy(),
            new WatercolorMarkerStrategy()
        };

        currentStrategy = strategies[brushSettings.StrategyIndex];

        if (EraseAction.action != null)
            EraseAction.action.performed += _ => ToggleEraseMode();

        if (UndoAction.action != null)
            UndoAction.action.performed += _ => Undo();
    }

    private void Update()
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
                    currentStrategy.Draw(whiteboard, touchPos, lastTouchPos, brushSettings.BrushSize, brushColors, isErasing);

                lastTouchPos = touchPos;
                touchedLastFrame = true;
                return;
            }
        }

        touchedLastFrame = false;
    }

    // Toggle between draw and erase modes
    private void ToggleEraseMode()
    {
        isErasing = !isErasing;
    } 

    // Undo the last stroke
    private void Undo()
    {
        if (whiteboard != null)
        {
            whiteboard.Undo();
        }
    }

    // Callback handlers for brush size changes (observed from BrushSettings)
    private void OnBrushSizeChanged(int size)
    {
        brushColors = Enumerable.Repeat(brushSettings.BrushColor, size * size).ToArray();
    }

    // Callback handlers for brush color changes (observed from BrushSettings)
    private void OnBrushColorChanged(Color color)
    {
        brushColors = Enumerable.Repeat(color, brushSettings.BrushSize * brushSettings.BrushSize).ToArray();
    }

    // Callback handlers for strategy index changes (observed from BrushSettings)
    private void OnStrategyChanged(int index)
    {
        if (index >= 0 && index < strategies.Count)
            currentStrategy = strategies[index];
    }
}
