using UnityEngine;
using System.Collections.Generic;

// Phasing changes visibility only: the trigger hurtbox remains vulnerable throughout.
public class Ghost : MonoBehaviour
{
    static readonly HashSet<Ghost> activeGhosts = new HashSet<Ghost>();
    [Min(.1f)] public float separationDistance = 1.8f;
    [Min(.1f)] public float separationSpeed = 8f;
    [Min(.01f)] public float visibleSeconds = .5f;
    [Min(.01f)] public float halfVisibleSeconds = .3f;
    [Range(0f, 1f)] public float halfVisibleAlpha = .5f;
    [Min(.01f)] public float hiddenSeconds = 1.1f;
    [Range(0f, 1f)] public float requiredPlayerOverlap = .3f;
    [Min(.01f)] public float damageInterval = .5f;
    [Min(0f)] public float contactDamage = 5f;
    EnemyMove movement;
    EnemyData data;
    Rigidbody2D body;
    SpriteRenderer[] visuals;
    Collider2D[] hitboxes;
    Player player;
    Collider2D playerBody;
    Transform target;
    float phase, nextDamageAt;
    bool dead;
    public float VisibilityAlpha
    {
        get
        {
            float half = Mathf.Max(.01f, halfVisibleSeconds);
            float fullEnd = half + Mathf.Max(.01f, visibleSeconds);
            if (phase < half) return halfVisibleAlpha;
            if (phase < fullEnd) return 1f;
            return phase < fullEnd + half ? halfVisibleAlpha : 0f;
        }
    }
    public bool IsRevealed => !dead && VisibilityAlpha > 0f;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetGhosts() { activeGhosts.Clear(); }
    void OnEnable() { activeGhosts.Add(this); }
    void OnDisable() { activeGhosts.Remove(this); }

    void Awake()
    {
        movement = GetComponent<EnemyMove>();
        if (movement != null) movement.enabled = false;
        data = GetComponent<EnemyData>();
        body = GetComponent<Rigidbody2D>();
        visuals = GetComponentsInChildren<SpriteRenderer>(true);
        hitboxes = GetComponentsInChildren<Collider2D>(true);
        foreach (var hitbox in hitboxes) hitbox.isTrigger = true;
        if (body != null)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.velocity = Vector2.zero;
        }
        SetOpacity(VisibilityAlpha);
    }

    void Start() { FindPlayer(); }
    void FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag("Player");
        player = go != null ? go.GetComponent<Player>() : null;
        playerBody = player != null ? player.GetComponent<Collider2D>() : null;
    }
    void Update()
    {
        if (dead) return;
        if (data != null && data.enemy_current_HP <= 0f)
        {
            dead = true;
            foreach (var hitbox in hitboxes) hitbox.enabled = false;
            if (body != null) { body.velocity = Vector2.zero; body.simulated = false; }
            SetOpacity(1f);
            Destroy(gameObject, .35f);
            return;
        }
        if (StageRewardController.InputBlocked || EnemyFrostStatus.IsFrozenOn(this)) return;
        phase = Mathf.Repeat(phase + Time.deltaTime, 2f * Mathf.Max(.01f, halfVisibleSeconds) + Mathf.Max(.01f, visibleSeconds) + Mathf.Max(.01f, hiddenSeconds));
    }
    void LateUpdate() { SetOpacity(dead ? 1f : VisibilityAlpha); }
    void SetOpacity(float alpha)
    {
        foreach (var visual in visuals) if (visual != null)
        {
            visual.enabled = alpha > 0f;
            var color = visual.color; color.a = alpha; visual.color = color;
        }
    }
    void FixedUpdate()
    {
        if (dead || data == null || data.enemy_current_HP <= 0f || StageRewardController.InputBlocked ||
            EnemyFrostStatus.IsFrozenOn(this) || GameManager.player_current_HP <= 0f) return;
        if (player == null || playerBody == null) FindPlayer();
        if (playerBody == null || body == null || hitboxes.Length == 0) return;
        target = LunarClone.ResolveTarget(hitboxes[0].bounds.center, player.transform);
        var echo = target != null ? target.GetComponent<LunarClone>() : null;
        Vector2 targetPoint = echo != null ? echo.AimPoint : (Vector2)playerBody.bounds.center;
        Vector2 direction = targetPoint - (Vector2)hitboxes[0].bounds.center;
        float speed = movement != null ? Mathf.Max(0f, movement.enemy_speed) : 5f;
        Vector2 next = body.position + Vector2.ClampMagnitude(direction, speed * EnemyFrostStatus.SpeedMultiplier(this) * Time.fixedDeltaTime);
        body.MovePosition(next + SeparationOffset(next));
        if (Mathf.Abs(direction.x) > .01f)
            foreach (var visual in visuals) if (visual != null) visual.flipX = direction.x < 0f;
    }
    public Vector2 SeparationOffset(Vector2 position)
    {
        Vector2 offset=Vector2.zero;
        foreach(var other in activeGhosts)
        {
            if(other==null || other==this || other.dead || other.data==null || other.data.enemy_current_HP<=0f || other.gameObject.scene!=gameObject.scene)continue;
            Vector2 otherPosition=other.body!=null?other.body.position:(Vector2)other.transform.position;
            Vector2 delta=position-otherPosition;float distance=delta.magnitude;
            float spacing=Mathf.Max(separationDistance,other.separationDistance);
            if(distance>=spacing)continue;
            Vector2 away=distance>.001f?delta/distance:GetInstanceID()<other.GetInstanceID()?Vector2.left:Vector2.right;
            offset+=away*(spacing-distance)*.5f;
        }
        return Vector2.ClampMagnitude(offset,separationSpeed*Time.fixedDeltaTime);
    }
    void OnTriggerStay2D(Collider2D other)
    {
        if (dead || data == null || data.enemy_current_HP <= 0f || Time.time < nextDamageAt ||
            StageRewardController.InputBlocked || EnemyFrostStatus.IsFrozenOn(this) || GameManager.player_current_HP <= 0f) return;
        var echo = other.GetComponentInParent<LunarClone>();
        if (echo != null)
        {
            if (echo.Health <= 0f || !echo.isActiveAndEnabled ||
                PlayerOverlapFraction(other) + .0001f < requiredPlayerOverlap) return;
            nextDamageAt = Time.time + Mathf.Max(.01f, damageInterval);
            echo.Hurt(contactDamage);
            return;
        }
        if (player == null) FindPlayer();
        if (player == null || other.GetComponentInParent<Player>() != player) return;
        if (PlayerOverlapFraction(playerBody) + .0001f < requiredPlayerOverlap) return;
        nextDamageAt = Time.time + Mathf.Max(.01f, damageInterval);
        GameManager.DamagePlayer(contactDamage);
    }

    // Sample the actual player/clone shape, rather than counting empty capsule corners.
    public float PlayerOverlapFraction(Collider2D target)
    {
        if (target == null || !target.enabled || hitboxes == null) return 0f;
        var bounds = target.bounds;
        int inside = 0, overlap = 0;
        const int samples = 12;
        for (int y = 0; y < samples; y++) for (int x = 0; x < samples; x++)
        {
            var point = new Vector2(bounds.min.x + (x + .5f) * bounds.size.x / samples,
                bounds.min.y + (y + .5f) * bounds.size.y / samples);
            if (!target.OverlapPoint(point)) continue;
            inside++;
            foreach (var hitbox in hitboxes)
                if (hitbox != null && hitbox.enabled && hitbox.OverlapPoint(point)) { overlap++; break; }
        }
        return inside > 0 ? overlap / (float)inside : 0f;
    }
}
