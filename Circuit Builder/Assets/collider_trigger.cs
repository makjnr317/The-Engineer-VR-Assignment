using UnityEngine;
using UnityEngine.UIElements;

public class collider_trigger : MonoBehaviour
{
    [SerializeField] public Box boxScript;

    private void OnTriggerStay(Collider other)
    {
        if (boxScript.circuit_status)
            boxScript.solve();
    }

    private void OnTriggerExit(Collider other)
    {
        if (boxScript.circuit_status)
            boxScript.solve();
    }
}
