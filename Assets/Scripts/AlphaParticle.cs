using UnityEngine;

public class AlphaParticle : MonoBehaviour
{
    private Vector3 velocity;
    private float life = 3f;
    private float age = 0f;
    private Renderer rend;
    private Color baseColor;
    private TrailRenderer trail;

    public void Init(Vector3 initialVelocity, float lifetime)
    {
        velocity = initialVelocity;
        life = lifetime;
    }

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        if (rend && rend.material.HasProperty("_Color"))
            baseColor = rend.material.color;

        trail = GetComponent<TrailRenderer>();
    }

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;
        velocity *= (1f - 0.15f * Time.deltaTime);

        age += Time.deltaTime;
        float k = Mathf.Clamp01(age / life);

        if (rend && rend.material.HasProperty("_Color"))
        {
            Color c = baseColor;
            c.a = 1f - k;
            rend.material.color = c;
        }

        if (trail)
        {
            trail.time = Mathf.Lerp(trail.time, 0.1f, Time.deltaTime * 0.5f);
        }

        if (age >= life)
        {
            Destroy(gameObject);
        }
    }
}
