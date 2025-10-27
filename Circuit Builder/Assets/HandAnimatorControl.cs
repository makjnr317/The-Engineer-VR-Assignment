//using UnityEngine;
//using UnityEngine.InputSystem;


//public class HandAnimatorControl : MonoBehaviour
//{
//    public InputActionProperty triggerValue;
//    public InputActionProperty gripValue;

//    public Animator handAnimator;
//    // Start is called once before the first execution of Update after the MonoBehaviour is created
//    void Start()
//    {

//    }

//    // Update is called once per frame
//    void Update()
//    {
//        float trigger = triggerValue.action.ReadValue<float>();
//        float grip = gripValue.action.ReadValue<float>();

//        handAnimator.SetFloat("Trigger", trigger);
//        handAnimator.SetFloat("Grip", grip);
//    }
//}

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HandAnimatorControl : MonoBehaviour
{
    [Header("Input Actions")]
    public InputActionProperty triggerValue;
    public InputActionProperty gripValue;

    [Header("Hand Settings")]
    public Animator handAnimator;
    public Transform handTransform;
    public LayerMask interactableLayer;
    public float grabRange = 0.5f;

    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private bool isGrabbing;

    void Start()
    {
        isGrabbing = false;
    }

    void Update()
    {
        // --- Animate hand based on controller inputs ---
        float trigger = triggerValue.action.ReadValue<float>();
        float grip = gripValue.action.ReadValue<float>();

        handAnimator.SetFloat("Trigger", trigger);
        handAnimator.SetFloat("Grip", grip);

        // --- Handle grabbing and releasing ---
        if (trigger > 0.8f && !isGrabbing)
        {
            TryGrabObject();
        }
        else if (trigger < 0.2f && isGrabbing)
        {
            ReleaseObject();
        }
    }

    private void TryGrabObject()
    {
        Collider[] colliders = Physics.OverlapSphere(handTransform.position, grabRange, interactableLayer);

        if (colliders.Length > 0)
        {
            heldObject = colliders[0].gameObject;
            heldRigidbody = heldObject.GetComponent<Rigidbody>();

            // Disable physics while held
            if (heldRigidbody != null)
                heldRigidbody.isKinematic = true;

            // Parent to hand
            heldObject.transform.SetParent(handTransform);
            heldObject.transform.localPosition = Vector3.zero;
            heldObject.transform.localRotation = Quaternion.identity;

            isGrabbing = true;
        }
    }

    private void ReleaseObject()
    {
        if (heldObject != null)
        {
            heldObject.transform.SetParent(null);

            if (heldRigidbody != null)
            {
                heldRigidbody.isKinematic = false; // Re-enable physics
                heldRigidbody = null;
            }

            // Trigger respawn if applicable
            GrabbableObject grabbable = heldObject.GetComponent<GrabbableObject>();
            if (grabbable != null)
            {
                StartCoroutine(RespawnAfterDelay(grabbable, 2f)); // wait 2s before checking
            }

            heldObject = null;
        }

        isGrabbing = false;
    }

    private IEnumerator RespawnAfterDelay(GrabbableObject grabbable, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Only reset if object has fallen below table height
        if (grabbable != null && grabbable.transform.position.y < 0.2f)
        {
            grabbable.ResetToStart();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (handTransform != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(handTransform.position, grabRange);
        }
    }
}
