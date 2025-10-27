using System.Collections.Generic;
using UnityEngine;

public class componentDrop : MonoBehaviour
{
    private Dictionary<GameObject, (Vector3 position, Quaternion rotation)> initialTransforms;

    // Tags to track
    [SerializeField] private string[] trackedTags = { "component", "buzzer", "led" };

    void Start()
    {
        initialTransforms = new Dictionary<GameObject, (Vector3, Quaternion)>();

        // Find all objects with the listed tags
        foreach (string tag in trackedTags)
        {
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in taggedObjects)
            {
                if (!initialTransforms.ContainsKey(obj))
                {
                    initialTransforms[obj] = (obj.transform.position, obj.transform.rotation);
                }
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;

        // Check if the collided object is one of the tracked ones
        if (initialTransforms.ContainsKey(other))
        {
            var (originalPosition, originalRotation) = initialTransforms[other];
            other.transform.SetPositionAndRotation(originalPosition, originalRotation);

            // Optional: reset velocity if it has a Rigidbody
            Rigidbody rb = other.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
               
            }
        }
    }

}
