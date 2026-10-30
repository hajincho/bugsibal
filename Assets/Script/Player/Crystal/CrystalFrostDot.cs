using UnityEngine;

public sealed class CrystalFrostDot : MonoBehaviour
{
    EnemyData enemy;
    float nextTick;
    public int RemainingTicks { get; private set; }
    void Awake(){enemy=GetComponent<EnemyData>();}
    public void Apply(){RemainingTicks=3;nextTick=Time.time+1f;enabled=true;}
    void Update()
    {
        if(enemy==null || enemy.enemy_current_HP<=0){RemainingTicks=0;enabled=false;return;}
        if(StageRewardController.InputBlocked) return;
        while(RemainingTicks>0 && Time.time>=nextTick)
        {
            enemy.TakeDamage(2f,CombatDamageKind.Skill);
            RemainingTicks--;nextTick+=1f;
        }
        if(RemainingTicks==0) enabled=false;
    }
}
