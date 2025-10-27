using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HighlightClosestCollider : MonoBehaviour
{
    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material originalMaterial; 
    [SerializeField] private float breadboardDistance = 0.35f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGrabbed = false;

    private GameObject breadboard;
    private Dictionary<Transform, GameObject> highlightedForChild = new Dictionary<Transform, GameObject>();

    private AudioManager audioManager;
    private static HashSet<GameObject> occupiedColliders = new HashSet<GameObject>();

    private HashSet<GameObject> occupiedByThis = new HashSet<GameObject>();

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();

    private GameObject[] cachedColliders;

    private Dictionary<Transform, Vector3> originalLegLocalPositions = new Dictionary<Transform, Vector3>();

    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrab);
            grabInteractable.selectExited.AddListener(OnRelease);
        }
        else
        {
            Debug.LogWarning($"[{name}] No XRGrabInteractable component found on object.");
        }

        breadboard = GameObject.Find("breadboard");
        if (breadboard == null)
            Debug.LogWarning("No GameObject named 'breadboard' found in scene!");

        audioManager = FindFirstObjectByType<AudioManager>();
        if (audioManager == null)
            audioManager = FindObjectOfType<AudioManager>();

        cachedColliders = GameObject.FindGameObjectsWithTag("colliders");

        foreach (Transform leg in transform)
        {
            if (leg.CompareTag("legs"))
                originalLegLocalPositions[leg] = leg.localPosition;
        }

    }

    private void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrab);
            grabInteractable.selectExited.RemoveListener(OnRelease);
        }
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        isGrabbed = true;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        foreach (var c in occupiedByThis)
            occupiedColliders.Remove(c);
        occupiedByThis.Clear();

        ClearAllHighlights();
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        if (breadboard == null)
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
            ClearAllHighlights();
            return;
        }

        float distToBoard = Vector3.Distance(transform.position, breadboard.transform.position);

        if (distToBoard <= breadboardDistance)
        {
            TrySnapToColliders();
        }
        else
        {
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }
        }

        ClearAllHighlights();
    }

    private void Update()
    {
        if (!isGrabbed || breadboard == null) return;

        float distToBoard = Vector3.Distance(transform.position, breadboard.transform.position);
        if (distToBoard > breadboardDistance)
        {
            ClearAllHighlights();
            return;
        }

        Dictionary<Transform, GameObject> closestMap = new Dictionary<Transform, GameObject>();

        foreach (Transform child in transform)
        {
            if (!child.CompareTag("legs"))
                continue;

            GameObject closest = FindClosestCollider(child);
            if (closest != null)
                closestMap[child] = closest;
        }

        var byTarget = closestMap.GroupBy(kvp => kvp.Value);
        var filteredClosestMap = new Dictionary<Transform, GameObject>();
        foreach (var g in byTarget)
        {
            var best = g.OrderBy(kvp => Vector3.Distance(kvp.Key.position, kvp.Value.transform.position)).First();
            filteredClosestMap[best.Key] = best.Value;
        }

        
        var toRemove = highlightedForChild.Keys.Except(filteredClosestMap.Keys).ToList();
        foreach (var k in toRemove)
        {
            RevertMaterial(highlightedForChild[k]);
            highlightedForChild.Remove(k);
        }

        foreach (var kvp in filteredClosestMap)
        {
            Transform child = kvp.Key;
            GameObject newClosest = kvp.Value;

            if (highlightedForChild.TryGetValue(child, out GameObject oldHighlighted))
            {
                if (oldHighlighted != newClosest)
                {
                    RevertMaterial(oldHighlighted);
                    highlightedForChild[child] = newClosest;
                    ApplyMaterial(newClosest);
                }
            }
            else
            {
                highlightedForChild[child] = newClosest;
                ApplyMaterial(newClosest);
            }
        }
    }

    private GameObject FindClosestCollider(Transform from)
    {
        var colliders = cachedColliders;
        if (colliders == null || colliders.Length == 0)
            colliders = GameObject.FindGameObjectsWithTag("colliders");

        GameObject closest = null;
        float minDist = Mathf.Infinity;

        foreach (var c in colliders)
        {
            if (occupiedColliders.Contains(c)) continue;
            float dist = Vector3.Distance(from.position, c.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = c;
            }
        }

        return closest;
    }

    private void ApplyMaterial(GameObject obj)
    {
        if (obj == null) return;
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalMaterials.ContainsKey(obj))
                originalMaterials[obj] = renderer.sharedMaterial != null ? renderer.sharedMaterial : originalMaterial;

            renderer.material = highlightMaterial;
        }
    }

    private void RevertMaterial(GameObject obj)
    {
        if (obj == null) return;
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (originalMaterials.TryGetValue(obj, out Material orig))
                renderer.material = orig;
            else if (originalMaterial != null)
                renderer.material = originalMaterial;
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var kvp in highlightedForChild)
            RevertMaterial(kvp.Value);
        highlightedForChild.Clear();
    }

    private void TrySnapToColliders()
    {
        var legs = transform.Cast<Transform>().Where(t => t.CompareTag("legs")).ToList();
        if (legs.Count == 0 || highlightedForChild.Count != legs.Count)
            return;

        var uniqueColliders = new HashSet<GameObject>(highlightedForChild.Values);
        if (uniqueColliders.Count != legs.Count || uniqueColliders.Any(c => occupiedColliders.Contains(c)))
            return;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        Vector3 currentEuler = transform.eulerAngles;
        float snappedY = Mathf.Round(Normalize360(currentEuler.y) / 90f) * 90f;
        float snappedZ = Mathf.Round(Normalize360(currentEuler.z) / 90f) * 90f;
        Quaternion finalRotation = Quaternion.Euler(270f, snappedY, snappedZ);

        Vector3 newParentPos = Vector3.zero;
        foreach (var kvp in highlightedForChild)
        {
            Transform leg = kvp.Key;
            GameObject target = kvp.Value;

            Vector3 legLocalPos = originalLegLocalPositions[leg];
            Vector3 requiredParentPos = target.transform.position - finalRotation * legLocalPos;
            newParentPos += requiredParentPos;
        }
        newParentPos /= legs.Count;

        newParentPos.y = -0.4977f;

        rb?.MovePosition(newParentPos);
        rb?.MoveRotation(finalRotation);

        if (audioManager != null)
            audioManager.PlaySFX(audioManager.snap);

        foreach (var target in highlightedForChild.Values)
        {
            occupiedColliders.Add(target);
            occupiedByThis.Add(target); 
        }

        ClearAllHighlights();

        Debug.Log($"Snapped to colliders. Final Rotation (Euler): {finalRotation.eulerAngles}");
    }

    private float Normalize360(float angle)
    {
        // faster & safe version
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}
