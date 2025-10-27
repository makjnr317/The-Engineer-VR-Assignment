using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class InteractorGrabTracker : MonoBehaviour
{
    public XRDirectInteractor directInteractor;

    private GameObject grabbedObject; // stores the currently held object

    void Awake()
    {
        // Subscribe to grab and release events
        directInteractor.selectEntered.AddListener(OnGrabbed);
        directInteractor.selectExited.AddListener(OnReleased);
    }

    void OnDestroy()
    {
        // Unsubscribe for safety
        directInteractor.selectEntered.RemoveListener(OnGrabbed);
        directInteractor.selectExited.RemoveListener(OnReleased);
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        grabbedObject = args.interactableObject.transform.gameObject;
        Debug.Log("Started grabbing: " + grabbedObject.name);
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (grabbedObject != null)
        {
            Debug.Log("Released: " + grabbedObject.name);
            grabbedObject = null;
        }
    }

    void Update()
    {
        // While holding something, you can continuously track or print its name
        if (grabbedObject != null)
        {
            Debug.Log("Currently holding: " + grabbedObject.name);
            // 👇 Example: do something else with it here
            // e.g. check its position, distance, or update UI text
        }
    }
}
