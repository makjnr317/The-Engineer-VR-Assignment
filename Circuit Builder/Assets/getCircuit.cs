using UnityEngine;

public class getCircuit : MonoBehaviour
{

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("legs"))
        {
            Debug.Log("Node overlapping: " + other.name);
        }
    }

}

