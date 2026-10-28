using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyData : MonoBehaviour
{
    public float enemy_HP;
    public float enemy_current_HP;
    public float enemy_power;
    SpriteRenderer sprite;
    bool initialized;
    Collider2D[] deathColliders;
    Rigidbody2D[] deathBodies;
    public bool IsBoss => GetComponent<Golem>()!=null || GetComponent<SwordMan>()!=null || name=="FinalBoss";
    void Start()
    {
        enemy_current_HP=enemy_HP;sprite=GetComponent<SpriteRenderer>();initialized=true;
        StageRewardController.Ensure();
        if(CombatPresentation.Instance!=null)CombatPresentation.Instance.Register(this);
    }
    void OnEnable(){if(initialized && CombatPresentation.Instance!=null)CombatPresentation.Instance.Register(this);}
    void OnDisable(){if(CombatPresentation.Instance!=null)CombatPresentation.Instance.Unregister(this);}
    void Update()
    {
        enemy_current_HP = Mathf.Clamp(enemy_current_HP, 0, Mathf.Max(0, enemy_HP));
        if (initialized && enemy_current_HP <= 0f) DisableDeathHitboxes();
    }

    void LateUpdate()
    {
        // Death animations can enable child attack objects/colliders again.
        if (initialized && enemy_current_HP <= 0f) DisableDeathHitboxes();
    }

    void DisableDeathHitboxes()
    {
        if (deathColliders == null) deathColliders = GetComponentsInChildren<Collider2D>(true);
        if (deathBodies == null) deathBodies = GetComponentsInChildren<Rigidbody2D>(true);
        foreach (var hitbox in deathColliders)
        {
            if (hitbox != null && hitbox.enabled && hitbox.GetComponentInParent<EnemyData>() == this)
                hitbox.enabled = false;
        }
        foreach (var body in deathBodies)
        {
            if (body == null || !body.simulated || body.GetComponentInParent<EnemyData>() != this) continue;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.simulated = false;
        }
    }
    public float TakeDamage(float amount,CombatDamageKind kind=CombatDamageKind.Normal)
    {
        if(float.IsNaN(amount)||float.IsInfinity(amount)||amount<=0 || enemy_current_HP<=0)return 0;
        float before=enemy_current_HP;
        enemy_current_HP=Mathf.Clamp(enemy_current_HP-amount,0,Mathf.Max(0,enemy_HP));
        if (enemy_current_HP <= 0f)
        {
            var frost = GetComponent<EnemyFrostStatus>();
            if (frost != null) frost.CancelStatus();
            DisableDeathHitboxes();
        }
        float applied=before-enemy_current_HP;
        CombatPresentation.ShowDamage(CombatPresentation.HeadPosition(transform),applied,kind);
        if(enemy_current_HP<=0 && IsBoss && SceneManager.GetActiveScene().name=="MidBoss" && FindObjectOfType<StageManager>()==null)
        {var controller=StageRewardController.Ensure();if(controller!=null)controller.MarkCleared();}
        return applied;
    }
    public float Heal(float amount)
    {
        if(amount<=0 || enemy_current_HP<=0)return 0;
        float before=enemy_current_HP;enemy_current_HP=Mathf.Min(enemy_HP,enemy_current_HP+amount);
        float healed=enemy_current_HP-before;
        CombatPresentation.ShowDamage(CombatPresentation.HeadPosition(transform),healed,CombatDamageKind.Heal);return healed;
    }
    public void Hitted()
    {if(sprite!=null){sprite.color=new Color(1f,167f/255f,167f/255f,1);CancelInvoke(nameof(ToOriginColor));Invoke(nameof(ToOriginColor),.5f);}}
    void ToOriginColor(){if(sprite!=null)sprite.color=Color.white;}
}
