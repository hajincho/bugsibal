using UnityEngine;

public sealed class QueenGatherPulse : MonoBehaviour
{
    readonly SpriteRenderer[] waves=new SpriteRenderer[1];
    float born,lifetime,radius,diameter;
    public static void Create(Vector2 center,float radius,float duration,Sprite sprite)
    {
        if(sprite==null)return;
        var go=new GameObject("QueenPurpleGatherPulse");go.transform.position=center;
        var pulse=go.AddComponent<QueenGatherPulse>();
        pulse.radius=radius;pulse.lifetime=Mathf.Max(.65f,duration);pulse.born=Time.time;
        pulse.diameter=Mathf.Max(sprite.bounds.size.x,sprite.bounds.size.y);
        for(int i=0;i<pulse.waves.Length;i++)
        {
            var wave=new GameObject("PurplePNGWave"+i);wave.transform.SetParent(go.transform,false);
            var renderer=wave.AddComponent<SpriteRenderer>();renderer.sprite=sprite;renderer.sortingOrder=4;
            pulse.waves[i]=renderer;
        }
        pulse.Draw(0);Destroy(go,pulse.lifetime+.1f);
    }
    void Update()
    {
        float t=(Time.time-born)/lifetime;
        if(t>=1){Destroy(gameObject);return;}
        Draw(t);
    }
    void Draw(float t)
    {
        for(int i=0;i<waves.Length;i++)
        {
            waves[i].transform.localScale=Vector3.one*(radius*2/diameter*Mathf.Clamp01(t));
            waves[i].transform.localRotation=Quaternion.identity;
            waves[i].color=new Color(1,1,1,Mathf.Clamp01((1-t)*8)*.8f);
        }
    }
}
