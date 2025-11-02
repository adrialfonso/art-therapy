using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

// Class to hold the state of each XR controller
public class XRControllerState
{
    public XRRayInteractor ray;
    public bool isDrawing;
    public Vector2 lastTouchPos;
    public bool touchedLastFrame;
    public bool savedUndoState;
}
