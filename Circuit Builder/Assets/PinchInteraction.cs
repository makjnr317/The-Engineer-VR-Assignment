// 10/23/2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class PinchInteraction : MonoBehaviour
{
    public Transform thumbTip;
    public Transform indexTip;
    public float pinchThreshold = 0.02f; // Adjust based on your hand model scale
    public GameObject targetObject;

    private XRDirectInteractor interactor;

    void Start()
    {
        interactor = GetComponent<XRDirectInteractor>();
    }

    void Update()
    {
        if (thumbTip && indexTip && targetObject)
        {
            float distance = Vector3.Distance(thumbTip.position, indexTip.position);

            if (distance < pinchThreshold)
            {
                // Perform interaction with the target object
                InteractWithTarget();
            }
        }
    }

    private void InteractWithTarget()
    {
        // Example: Toggle the target object's active state
        targetObject.SetActive(!targetObject.activeSelf);
    }
}