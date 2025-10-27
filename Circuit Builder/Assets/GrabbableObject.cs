using UnityEngine;
using System.Collections;

public class GrabbableObject : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Renderer[] renderers;
    private float fadeDuration = 0.5f;

    private void Start()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void ResetToStart()
    {
        StartCoroutine(FadeOutAndRespawn());
    }

    private IEnumerator FadeOutAndRespawn()
    {
        yield return StartCoroutine(Fade(1f, 0f)); // fade out

        // Teleport
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
        }

        transform.SetParent(null);
        transform.position = startPosition;
        transform.rotation = startRotation;

        yield return StartCoroutine(Fade(0f, 1f)); // fade in
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);

            foreach (Renderer rend in renderers)
            {
                foreach (Material mat in rend.materials)
                {
                    if (mat.HasProperty("_Color"))
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;

                        // Ensure material supports transparency
                        if (alpha < 1f)
                        {
                            mat.SetFloat("_Mode", 3); // Transparent mode (if using Standard shader)
                            mat.renderQueue = 3000;
                        }
                    }
                }
            }
            yield return null;
        }
    }
}
