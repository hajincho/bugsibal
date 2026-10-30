using UnityEngine;
using System.Collections.Generic;

public static class CrystalEffects
{
    public static Vector2 TargetPoint(EnemyData enemy)
    {
        var body=enemy!=null ? enemy.GetComponent<Collider2D>() : null;
        return body!=null ? (Vector2)body.bounds.center : enemy!=null ? (Vector2)enemy.transform.position : Vector2.zero;
    }
    public static List<EnemyData> Targets(Vector2 center,float radius,EnemyData primary=null)
    {
        var found=new HashSet<EnemyData>();
        if(primary!=null && primary.enemy_current_HP>0f) found.Add(primary);
        foreach(var collider in Physics2D.OverlapCircleAll(center,radius))
        {
            var enemy=collider.GetComponentInParent<EnemyData>();
            if(enemy!=null && enemy.CompareTag("Enemy") && enemy.enemy_current_HP>0f) found.Add(enemy);
        }
        return new List<EnemyData>(found);
    }
    public static void BurstSparkles(Vector2 position,float radius,int color,Material material)
    {
        if(material==null)return;
        Color tint=Color.Lerp(CrystalBowSkill.ColorAt(color),Color.white,.25f);
        float phase=Random.Range(0f,360f);
        const int count=24;
        for(int i=0;i<count;i++)
        {
            float angle=(phase+(i+Random.Range(-.35f,.35f))*360f/count)*Mathf.Deg2Rad;
            Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            Vector2 start=position+direction*Random.Range(0f,radius*.16f);
            CrystalSparkle.Create(start,material,direction*Random.Range(radius*.6f,radius*1.5f),
                Random.Range(1.078f,2.352f),Random.Range(1.44f,2.76f),i%2==0 ? Color.white : tint);
        }
    }
    public static void Burst(int color,Vector2 position,float radius,Material material,
        EnemyData primary=null,float visualScale=1f,bool icicleBurst=false,Sprite explosionSprite=null)
    {
        var root=new GameObject("CrystalBurst_"+color);
        root.transform.position=position;
        if(color!=0 && explosionSprite!=null)
            root.AddComponent<CrystalElementBurst>().Initialize(explosionSprite,radius,color);
        else
            root.AddComponent<ArtemisiaBurst>().InitializeColored(material,radius,CrystalBowSkill.ColorAt(color),visualScale);
        var targets=Targets(position,radius,primary);
        var pulls=new List<EnemyBowPull>();
        foreach(var enemy in targets)
        {
            // Deliberately bypass the orbit's healing-hit counter and all life-steal callbacks.
            enemy.TakeDamage(color==0 ? (icicleBurst ? 20f : 40f) : 5f,CombatDamageKind.Skill);
            if(enemy.enemy_current_HP<=0f) continue;
            if(color==0)
            {
                var pull=enemy.GetComponent<EnemyBowPull>();
                if(pull==null) pull=enemy.gameObject.AddComponent<EnemyBowPull>();
                pull.PullTo(position,3f,.3f);
                pulls.Add(pull);
            }
            else if(color==1)
            {
                var frost=enemy.GetComponent<EnemyFrostStatus>();
                if(frost==null) frost=enemy.gameObject.AddComponent<EnemyFrostStatus>();
                frost.Freeze(.4f);
                var dot=enemy.GetComponent<CrystalFrostDot>();
                if(dot==null) dot=enemy.gameObject.AddComponent<CrystalFrostDot>();
                dot.Apply();
            }

        }
        if(color==2)
        {
            EnemyData chosen=primary!=null && primary.enemy_current_HP>0f ? primary : null;
            float nearest=float.PositiveInfinity;
            if(chosen==null)foreach(var enemy in targets)
            {
                if(enemy.enemy_current_HP<=0f)continue;
                float distance=(TargetPoint(enemy)-position).sqrMagnitude;
                if(distance<nearest){nearest=distance;chosen=enemy;}
            }
            if(chosen!=null)
            {
                var shock=chosen.GetComponent<CrystalShock>();
                if(shock==null)shock=chosen.gameObject.AddComponent<CrystalShock>();
                shock.Apply(material);
            }
        }
        for(int i=0;i<pulls.Count;i++) for(int j=i+1;j<pulls.Count;j++) pulls[i].IgnoreCollisionsWith(pulls[j]);
    }
}
