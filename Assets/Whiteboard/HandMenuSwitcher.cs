using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class HandMenuSwitcher : MonoBehaviour
{
    [Header("Canvas to move")]
    public GameObject canvasObject;

    [Header("XR Controllers")]
    public ActionBasedController rightHandController;
    public ActionBasedController leftHandController;

    [Header("Input Actions")]
    public InputActionProperty ShowMenuLeftAction;
    public InputActionProperty ShowMenuRightAction;

    private XRRayInteractor rightRay;
    private XRRayInteractor leftRay;

    // State tracking
    private Transform currentHand = null; 
    private bool canvasActive = false; 

    private void Start()
    {
        rightRay = rightHandController.GetComponent<XRRayInteractor>();
        leftRay = leftHandController.GetComponent<XRRayInteractor>();

        if (ShowMenuLeftAction.action != null)
            ShowMenuLeftAction.action.performed += _ => ToggleLeft();

        if (ShowMenuRightAction.action != null)
            ShowMenuRightAction.action.performed += _ => ToggleRight();
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
        if (canvasActive && currentHand == targetHand)
        {
            // Second press on the same hand -> only hide the menu
            canvasObject.SetActive(false);
            canvasActive = false;
            currentHand = null;
        }
        else
        {
            // Always re-enable the ray of the other hand
            if (otherRay != null) 
            {
                otherRay.enabled = true;
            }

            // Attach canvas to the pressed hand and disable its ray
            canvasObject.SetActive(true);
            Attach(canvasObject, targetHand);
            
            if (targetRay != null) 
            {
                targetRay.enabled = false;
            }

            currentHand = targetHand;
            canvasActive = true;
        }
    }

    private void Attach(GameObject obj, Transform parentTransform)
    {
        obj.transform.SetParent(parentTransform);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localScale = new Vector3(0.003f, 0.003f, 0.003f);
    }
}
