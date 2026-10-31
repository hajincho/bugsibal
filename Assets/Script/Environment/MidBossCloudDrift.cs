using UnityEngine;

public sealed class MidBossCloudDrift : MonoBehaviour
{
    public Transform[] clouds;
    public float left = -95f, right = 105f;
    Vector3[] origins;
    void Awake()
    {
        origins = new Vector3[clouds.Length];
        for (int i = 0; i < clouds.Length; i++) origins[i] = clouds[i].localPosition;
    }
    void Update()
    {
        for (int i = 0; i < clouds.Length; i++)
        {
            if (clouds[i] == null) continue;
            Vector3 p = origins[i];
            p.x = left + Mathf.Repeat(p.x - left + Time.timeSinceLevelLoad * (.12f + i % 5 * .035f), right - left);
            p.y += Mathf.Sin(Time.timeSinceLevelLoad * .18f + i * 1.7f) * .15f;
            clouds[i].localPosition = p;
        }
    }
}
