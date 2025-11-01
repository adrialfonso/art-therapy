using System;
using UnityEngine;
using UnityEngine.InputSystem;

// This class observes input events from an XR controller and raises corresponding events for drawing, erasing, and undoing actions.
public class DrawEventObserver : MonoBehaviour
{
    public event Action OnDrawPressed;
    public event Action OnDrawReleased;
    public event Action OnErasePressed;
    public event Action OnUndoPressed;

    [Header("XRI Controller Actions")]
    [SerializeField] private InputActionProperty DrawAction;
    [SerializeField] private InputActionProperty EraseAction;
    [SerializeField] private InputActionProperty UndoAction;

    private bool wasDrawingLastFrame = false;

    private void Awake()
    {
        if (EraseAction.action != null) 
            EraseAction.action.performed += _ => OnErasePressed?.Invoke();
        if (UndoAction.action != null) 
            UndoAction.action.performed += _ => OnUndoPressed?.Invoke();
    }

    private void Update()
    {
        if (DrawAction.action == null) return;

        float drawValue = DrawAction.action.ReadValue<float>();
        bool isDrawing = drawValue > 0.1f;

        if (isDrawing && !wasDrawingLastFrame)
        {
            OnDrawPressed?.Invoke();
        }
        else if (!isDrawing && wasDrawingLastFrame)
        {
            OnDrawReleased?.Invoke();
        }

        wasDrawingLastFrame = isDrawing;
    }
}
