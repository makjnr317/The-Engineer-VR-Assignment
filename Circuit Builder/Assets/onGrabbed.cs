using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HighlightClosestCollider : MonoBehaviour
{
    private enum SnapOrientation { None, Horizontal, Vertical }

    [SerializeField] private Material highlightMaterial;
    [SerializeField] private Material originalMaterial;
    [SerializeField] private float breadboardDistance = 0.35f;

    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private bool isGrabbed = false;
    [SerializeField] public Box boxScript;

    private GameObject breadboard;
    [SerializeField] private AudioManager audioManager;

    private readonly Dictionary<string, int> objectSpanRules = new Dictionary<string, int>
    {
        {"L1", 2}, {"L2", 2}, {"L3", 2},
        {"220R", 4}, {"220R_1", 4}, {"220R_2", 4},
        {"wire1", 3}, {"wire2", 3}, {"wire3", 3},
        {"wire4", 4}, {"wire5", 4}, {"wire6", 4},
        {"wire7", 3}, {"wire8", 3}, {"wire9", 3},
        {"wire10", 4}, {"wire11", 4}
    };

    private int requiredSpan;

    private List<GameObject> validHighlightedColliders = new List<GameObject>();
    private SnapOrientation currentSnapOrientation = SnapOrientation.None; // State variable to store the orientation.

    private static HashSet<GameObject> occupiedColliders = new HashSet<GameObject>();
    private HashSet<GameObject> occupiedByThis = new HashSet<GameObject>();

    private Dictionary<GameObject, Material> originalMaterials = new Dictionary<GameObject, Material>();
    private GameObject[] cachedColliders;
    private Dictionary<string, GameObject> colliderMap = new Dictionary<string, GameObject>();
    private Dictionary<Transform, Vector3> originalLegLocalPositions = new Dictionary<Transform, Vector3>();

    private List<Transform> legs = new List<Transform>();

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

        breadboard = GameObject.Find("breadboard");
        audioManager = FindFirstObjectByType<AudioManager>();

        cachedColliders = GameObject.FindGameObjectsWithTag("colliders");

        foreach (var col in cachedColliders)
        {
            colliderMap[col.name] = col;
        }

        foreach (Transform child in transform)
        {
            if (child.CompareTag("legs"))
            {
                legs.Add(child);
                originalLegLocalPositions[child] = child.localPosition;
            }
        }

        if (gameObject.name.StartsWith("L"))
        {
            legs = legs.OrderBy(l => l.name).ToList();
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
        rb.isKinematic = true;
        rb.useGravity = false;

        var previouslyOccupied = new HashSet<GameObject>(occupiedByThis);

        foreach (var c in occupiedByThis)
        {
            occupiedColliders.Remove(c);
        }
        occupiedByThis.Clear();

        if (!objectSpanRules.TryGetValue(gameObject.name, out requiredSpan))
        {
            requiredSpan = 2;
        }

        ClearAllHighlights();

        if (boxScript != null)
        {
            boxScript.SolveWithManualChanges(previouslyOccupied, null);
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        isGrabbed = false;

        if (breadboard != null && Vector3.Distance(transform.position, breadboard.transform.position) <= breadboardDistance && validHighlightedColliders.Count > 0 && currentSnapOrientation != SnapOrientation.None)
        {
            TrySnapToColliders();
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        ClearAllHighlights();
    }

    private void Update()
    {
        if (!isGrabbed || breadboard == null || legs.Count < 2) return;

        if (Vector3.Distance(transform.position, breadboard.transform.position) > breadboardDistance)
        {
            ClearAllHighlights();
            return;
        }

        List<GameObject> bestColliderSet = FindBestValidColliderSet();

        if (!AreColliderSetsEqual(validHighlightedColliders, bestColliderSet))
        {
            ClearAllHighlights();
            validHighlightedColliders = bestColliderSet;
            foreach (var col in validHighlightedColliders)
            {
                ApplyMaterial(col);
            }
        }
    }

    private List<GameObject> FindBestValidColliderSet()
    {
        GameObject closestColliderToFirstLeg = FindClosestAvailableCollider(legs[0]);
        if (closestColliderToFirstLeg == null)
        {
            currentSnapOrientation = SnapOrientation.None;
            return new List<GameObject>();
        }

        if (!TryParseColliderName(closestColliderToFirstLeg.name, out Vector2Int startCoords))
        {
            currentSnapOrientation = SnapOrientation.None;
            return new List<GameObject>();
        }

        var allPossibleSets = new List<List<GameObject>>();
        FindValidSetsInDirection(startCoords, 1, 0, allPossibleSets); // Horizontal sets
        FindValidSetsInDirection(startCoords, 0, 1, allPossibleSets); // Vertical sets

        float minDistance = float.MaxValue;
        List<GameObject> bestSet = new List<GameObject>();

        foreach (var set in allPossibleSets)
        {
            if (set.Count == legs.Count)
            {
                if (legs.Count == 2)
                {
                    float dist1 = Vector3.Distance(legs[0].position, set[0].transform.position) +
                                  Vector3.Distance(legs[1].position, set[1].transform.position);

                    float dist2 = Vector3.Distance(legs[0].position, set[1].transform.position) +
                                  Vector3.Distance(legs[1].position, set[0].transform.position);

                    if (dist1 < minDistance)
                    {
                        minDistance = dist1;
                        bestSet = set; 
                    }

                    if (dist2 < minDistance)
                    {
                        minDistance = dist2;
                        bestSet = new List<GameObject> { set[1], set[0] };
                    }
                }
                else 
                {
                    float totalDist = 0f;
                    for (int i = 0; i < legs.Count; i++)
                    {
                        totalDist += Vector3.Distance(legs[i].position, set[i].transform.position);
                    }

                    if (totalDist < minDistance)
                    {
                        minDistance = totalDist;
                        bestSet = set;
                    }
                }
            }
        }

        if (bestSet.Count >= 2)
        {
            TryParseColliderName(bestSet[0].name, out Vector2Int firstPos);
            TryParseColliderName(bestSet[1].name, out Vector2Int secondPos);

            if (firstPos.y == secondPos.y)
            {
                currentSnapOrientation = SnapOrientation.Horizontal;
            }
            else 
            {
                currentSnapOrientation = SnapOrientation.Vertical;
            }
        }
        else
        {
            currentSnapOrientation = SnapOrientation.None;
        }

        return bestSet;
    }

    private void FindValidSetsInDirection(Vector2Int startCoords, int dCol, int dRow, List<List<GameObject>> validSets)
    {
        var set1 = GetColliderSet(startCoords, dCol, dRow);
        if (set1.Count == legs.Count) validSets.Add(set1);

        var set2 = GetColliderSet(startCoords, -dCol, -dRow);
        if (set2.Count == legs.Count) validSets.Add(set2);
    }

    private List<GameObject> GetColliderSet(Vector2Int start, int dCol, int dRow)
    {
        var set = new List<GameObject>();
        var startCollider = GetColliderAt(start.x, start.y);
        if (startCollider == null || occupiedColliders.Contains(startCollider)) return set;

        set.Add(startCollider);

        for (int i = 1; i < legs.Count; i++)
        {
            int targetCol = start.x;
            int targetRow = start.y;

            if (dCol != 0)
            {
                targetCol = start.x + dCol * (requiredSpan - 1);
            }
            else if (dRow != 0)
            {
                int holesToMove = requiredSpan - 1;
                int movesMade = 0;
                int gridSteps = 0;

                var gapStarts = new HashSet<int> { 0, 4, 8 };

                int currentRow = start.y;
                bool possible = true;

                while (movesMade < holesToMove)
                {
                    int nextRow = currentRow + dRow;
                    int moveCost = 1;
                    if ((dRow > 0 && gapStarts.Contains(currentRow)) || (dRow < 0 && gapStarts.Contains(nextRow)))
                    {
                        moveCost = 2;
                    }
                    if (movesMade + moveCost > holesToMove)
                    {
                        possible = false;
                        break;
                    }
                    movesMade += moveCost;
                    currentRow = nextRow;
                    gridSteps++;
                }

                if (!possible) return new List<GameObject>();

                targetRow = start.y + dRow * gridSteps;
            }

            GameObject endCollider = GetColliderAt(targetCol, targetRow);
            if (endCollider != null && !occupiedColliders.Contains(endCollider))
            {
                set.Add(endCollider);
            }
            else
            {
                return new List<GameObject>();
            }
        }
        return set;
    }

    private void TrySnapToColliders()
    {
        if (validHighlightedColliders.Count != legs.Count || currentSnapOrientation == SnapOrientation.None) return;

        rb.isKinematic = true;
        rb.useGravity = false;

        float snappedY = 0f;

        if (legs.Count == 2 && TryParseColliderName(validHighlightedColliders[0].name, out Vector2Int startCoords) && TryParseColliderName(validHighlightedColliders[1].name, out Vector2Int endCoords))
        {
            if (currentSnapOrientation == SnapOrientation.Horizontal)
            {
                snappedY = (endCoords.x > startCoords.x) ? 90f : 270f;
            }
            else
            {

                snappedY = (endCoords.y > startCoords.y) ? 0f : 180f;
            }
        }
        else
        {
            if (currentSnapOrientation == SnapOrientation.Horizontal)
            {
                snappedY = 90f;
            }
            else if (currentSnapOrientation == SnapOrientation.Vertical)
            {
                snappedY = 0f;
            }
        }

        Quaternion finalRotation = Quaternion.Euler(270f, snappedY, 0f);


        Vector3 newParentPos = Vector3.zero;
        for (int i = 0; i < legs.Count; i++)
        {
            Transform leg = legs[i];
            GameObject target = validHighlightedColliders[i];
            Vector3 legLocalPos = originalLegLocalPositions[leg];
            Vector3 requiredParentPos = target.transform.position - finalRotation * legLocalPos;
            newParentPos += requiredParentPos;
        }
        newParentPos /= legs.Count;
        newParentPos.y = -0.4843f;

        rb?.MovePosition(newParentPos);
        rb?.MoveRotation(finalRotation);

        if (audioManager != null)
            audioManager.PlaySFX(audioManager.snap);

        var collidersToAdd = new Dictionary<GameObject, string>();
        for (int i = 0; i < legs.Count; i++)
        {
            collidersToAdd[validHighlightedColliders[i]] = legs[i].name;
        }


        if (boxScript != null)
        {
            boxScript.SolveWithManualChanges(null, collidersToAdd);
        }

        foreach (var target in validHighlightedColliders)
        {
            occupiedColliders.Add(target);
            occupiedByThis.Add(target);
        }

        OccupyInBetweenColliders();
        ClearAllHighlights();


    }

    private GameObject GetColliderAt(int col, int row)
    {
        string name = $"{row}_{col}";
        colliderMap.TryGetValue(name, out GameObject obj);
        return obj;
    }

    private GameObject FindClosestAvailableCollider(Transform from)
    {
        GameObject closest = null;
        float minDist = Mathf.Infinity;
        foreach (var c in cachedColliders)
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

    private void OccupyInBetweenColliders()
    {
        if (validHighlightedColliders.Count < 2) return;
        if (TryParseColliderName(validHighlightedColliders.First().name, out Vector2Int startCoords) &&
            TryParseColliderName(validHighlightedColliders.Last().name, out Vector2Int endCoords))
        {
            int dCol = endCoords.x - startCoords.x;
            int dRow = endCoords.y - startCoords.y;
            int steps = Mathf.Max(Mathf.Abs(dCol), Mathf.Abs(dRow));
            if (steps == 0) return;
            int stepCol = dCol / steps;
            int stepRow = dRow / steps;
            for (int i = 1; i < steps; i++)
            {
                int col = startCoords.x + i * stepCol;
                int row = startCoords.y + i * stepRow;
                GameObject inBetweenCollider = GetColliderAt(col, row);
                if (inBetweenCollider != null)
                {
                    occupiedColliders.Add(inBetweenCollider);
                    occupiedByThis.Add(inBetweenCollider);
                }
            }
        }
    }

    private bool TryParseColliderName(string name, out Vector2Int coords)
    {
        coords = Vector2Int.zero;
        var parts = name.Split('_');
        if (parts.Length == 2 && int.TryParse(parts[0], out int row) && int.TryParse(parts[1], out int col))
        {
            coords = new Vector2Int(col, row);
            return true;
        }
        return false;
    }

    private bool AreColliderSetsEqual(List<GameObject> setA, List<GameObject> setB)
    {
        if (setA.Count != setB.Count) return false;
        return new HashSet<GameObject>(setA).SetEquals(new HashSet<GameObject>(setB));
    }

    #region Material Handling
    private void ApplyMaterial(GameObject obj)
    {
        if (obj == null) return;
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            if (!originalMaterials.ContainsKey(obj))
                originalMaterials[obj] = renderer.sharedMaterial;
            renderer.material = highlightMaterial;
        }
    }

    private void RevertMaterial(GameObject obj)
    {
        if (obj == null) return;
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null && originalMaterials.TryGetValue(obj, out Material orig))
        {
            renderer.material = orig;
        }
    }

    private void ClearAllHighlights()
    {
        foreach (var col in validHighlightedColliders)
            RevertMaterial(col);
        validHighlightedColliders.Clear();
        currentSnapOrientation = SnapOrientation.None;
    }
    #endregion
}