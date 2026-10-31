using UnityEngine;
using TMPro;
using System.Collections.Generic;

// Uses a finite reference max HP for percentage-based attacks, but never consumes HP.
[DefaultExecutionOrder(12000)]
[RequireComponent(typeof(EnemyData))]
public sealed class TrainingDummy : MonoBehaviour
{
    public float referenceMaxHealth=1000f;
    public string displayName="더미";
    public TMP_Text readout;
    public double TotalDamage { get; private set; }
    public int HitCount { get; private set; }
    public float LastDamage { get; private set; }
    struct Hit { public float at,damage; }
    readonly Queue<Hit> recent=new Queue<Hit>();
    EnemyData enemy;
    Rigidbody2D body;
    Vector3 home;
    float firstHit=-1f,recentDamage,nextLabelAt;
    public float DamagePerSecond
    {
        get
        {
            Trim();
            return recent.Count==0 ? 0f : recentDamage/Mathf.Clamp(Time.time-firstHit,.2f,5f);
        }
    }
    void Awake()
    {
        enemy=GetComponent<EnemyData>();body=GetComponent<Rigidbody2D>();home=transform.position;
        enemy.enemy_HP=enemy.enemy_current_HP=Mathf.Max(1f,referenceMaxHealth);enemy.enemy_power=0f;
    }
    public float TakeHit(float damage,CombatDamageKind kind)
    {
        if(damage<=0f || float.IsNaN(damage) || float.IsInfinity(damage))return 0f;
        Trim();if(firstHit<0f || recent.Count==0)firstHit=Time.time;
        LastDamage=damage;TotalDamage+=damage;HitCount++;
        recent.Enqueue(new Hit{at=Time.time,damage=damage});recentDamage+=damage;
        CombatPresentation.ShowDamage(CombatPresentation.HeadPosition(transform),damage,kind);
        RefreshLabel();
        return damage;
    }
    void Trim()
    {
        while(recent.Count>0 && Time.time-recent.Peek().at>5f)recentDamage-=recent.Dequeue().damage;
        recentDamage=Mathf.Max(0f,recentDamage);
    }
    void RefreshLabel()
    {
        if(readout!=null)readout.text=displayName+"  <color=#FFE08A>무한</color>\n누적 "+TotalDamage.ToString("0.0")+
            "\nDPS "+DamagePerSecond.ToString("0.0");
        nextLabelAt=Time.time+.1f;
    }
    void LateUpdate()
    {
        enemy.enemy_HP=enemy.enemy_current_HP=Mathf.Max(1f,referenceMaxHealth);
        transform.position=home;
        if(body!=null){body.position=home;body.velocity=Vector2.zero;body.angularVelocity=0f;}
        var frost=GetComponent<EnemyFrostStatus>();if(frost!=null && frost.IsFrozen)frost.MoveFrozenAnchor(home);
        if(Time.time>=nextLabelAt)RefreshLabel();
    }
}
