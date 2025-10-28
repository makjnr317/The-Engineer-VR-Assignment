using UnityEngine;

public class LightToggle : MonoBehaviour
{
    [SerializeField] private Light targetLight;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();
    }

    public void ToggleLight()
    {
        if (targetLight != null)
            targetLight.enabled = !targetLight.enabled;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            ToggleLight();
        }
    }
}
