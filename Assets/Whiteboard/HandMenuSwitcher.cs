using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class HandMenuSwitcher : MonoBehaviour
{
    [Header("Canvases to move")]
    public GameObject canvas2D;
    public GameObject canvas3D;

    [Header("XR Controllers")]
    public ActionBasedController rightHandController;
    public ActionBasedController leftHandController;

    [Header("Input Actions")]
    public InputActionProperty ShowMenuLeftAction;
    public InputActionProperty ShowMenuRightAction;

    [Header("Brush Controller Reference")]
    public BrushController brushController;

    private XRRayInteractor rightRay;
    private XRRayInteractor leftRay;

    // State tracking
    private Transform currentHand = null;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int sameHandClickCount = 0;

    private void Start()
    {
        rightRay = rightHandController.GetComponent<XRRayInteractor>();
        leftRay = leftHandController.GetComponent<XRRayInteractor>();

        // Keep original position for resetting
        originalPosition = GetActiveCanvas().transform.position;
        originalRotation = GetActiveCanvas().transform.rotation;

        if (ShowMenuLeftAction.action != null)
            ShowMenuLeftAction.action.performed += _ => ToggleLeft();

        if (ShowMenuRightAction.action != null)
            ShowMenuRightAction.action.performed += _ => ToggleRight();
    }

    private GameObject GetActiveCanvas()
    {
        if (brushController.is3DMode)
            return canvas3D;
        else
            return canvas2D;
    }

    private void ToggleRight()
    {
        ToggleHand(rightHandController.transform, rightRay, leftRay);
    }

    private void ToggleLeft()
    {
        ToggleHand(leftHandController.transform, leftRay, rightRay);
    }

    private void ToggleHand(Transform targetHand, XRRayInteractor targetRay, XRRayInteractor otherRay)
    {
        GameObject canvasObject = GetActiveCanvas();

        if (currentHand == targetHand)
        {
            sameHandClickCount++;

            if (sameHandClickCount == 1)
            {
                // First click -> attach menu to the same hand
                canvasObject.SetActive(true);
                Attach(canvasObject, targetHand);
                if (targetRay != null) targetRay.enabled = false;
                if (otherRay != null) otherRay.enabled = true;
            }
            else if (sameHandClickCount == 2)
            {
                // Second click -> just hide menu
                canvasObject.SetActive(false);
                if (targetRay != null) targetRay.enabled = true;
            }
            else
            {
                // Third click -> reset to original position
                canvasObject.SetActive(true);
                canvasObject.transform.SetParent(null);
                canvasObject.transform.position = originalPosition;
                canvasObject.transform.rotation = originalRotation;
                canvasObject.transform.localScale = new Vector3(0.007f, 0.007f, 0.007f);
                sameHandClickCount = 0;
                currentHand = null;
            }
        }
        else
        {
            // Change to the other hand
            sameHandClickCount = 1;

            // Always re-enable the other hand's ray
            if (otherRay != null) otherRay.enabled = true;

            // Attach canvas to the new hand
            canvasObject.SetActive(true);
            Attach(canvasObject, targetHand);
            if (targetRay != null) targetRay.enabled = false;

            currentHand = targetHand;
        }
    }

    private void Attach(GameObject obj, Transform parentTransform)
    {
        obj.transform.SetParent(parentTransform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.Euler(-30, 180, 0);
        obj.transform.localScale = new Vector3(0.0035f, 0.0035f, 0.0035f);
    }
}
