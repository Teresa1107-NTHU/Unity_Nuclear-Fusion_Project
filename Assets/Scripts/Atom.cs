using System.Collections;
using UnityEngine;

public class Atom : MonoBehaviour
{
    [Header("Shell")]
    public Renderer shellRenderer; // assign Shell's MeshRenderer
    public float shellTargetAlpha = 0.15f;

    [Header("Nucleons")]
    public Transform[] protons;   // optional list of proton transforms (for color)
    public Transform[] neutrons;

    private Material shellMat;
    private Color shellBaseColor;

    private void Awake()
    {
        if (shellRenderer)
        {
            shellMat = shellRenderer.material;
            if (shellMat.HasProperty("_Color"))
            {
                shellBaseColor = shellMat.color;
                // ensure starting fully transparent
                Color c = shellBaseColor;
                c.a = 0f;
                shellMat.color = c;
            }
        }
        // optionally set nucleon colors via their materials (already done by prefabs)
    }

    public IEnumerator FadeInShell(float duration)
    {
        if (shellMat == null) yield break;
        float t = 0f;
        Color c = shellBaseColor;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            c.a = Mathf.Lerp(0f, shellTargetAlpha, k);
            shellMat.color = c;
            yield return null;
        }
        c.a = shellTargetAlpha;
        shellMat.color = c;
    }

    public IEnumerator FadeOutShell(float duration)
    {
        if (shellMat == null) yield break;
        float t = 0f;
        Color c = shellMat.color;
        float startA = c.a;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            c.a = Mathf.Lerp(startA, 0f, k);
            shellMat.color = c;
            yield return null;
        }
        c.a = 0f;
        shellMat.color = c;
    }

    // optional: small breathing animation
    public IEnumerator Pulse(float duration, float scaleFactor = 1.05f)
    {
        Vector3 start = transform.localScale;
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Sin(t * Mathf.PI * 2f / duration) * 0.5f + 0.5f;
            transform.localScale = start * Mathf.Lerp(1f, scaleFactor, k);
            yield return null;
        }
        transform.localScale = start;
    }
}
