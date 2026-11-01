using UnityEngine;

// Subtle heat flicker stays on exterior scenery and never changes colliders.
public sealed class HellEnvironmentAnimator : MonoBehaviour
{
    public SpriteRenderer[] fires;
    public SpriteRenderer[] lavaPools;
    void Update()
    {
        float t = Time.time;
        if (fires != null)
            for (int i = 0; i < fires.Length; i++)
                if (fires[i] != null)
                {
                    float light = .92f + .05f * Mathf.Sin(t * 7.3f + i * 2.1f) + .03f * Mathf.Sin(t * 13.7f + i);
                    fires[i].color = new Color(1f, light, .88f + light * .12f, 1f);
                }
        if (lavaPools != null)
            for (int i = 0; i < lavaPools.Length; i++)
                if (lavaPools[i] != null)
                {
                    float light = .94f + .06f * Mathf.Sin(t * 1.7f + i * 1.8f);
                    lavaPools[i].color = new Color(1f, light, light, 1f);
                }
    }
}
