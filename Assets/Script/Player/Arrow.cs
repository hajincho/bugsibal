using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Arrow : MonoBehaviour
{
    public bool HasHit { get; private set; }
    public float Damage { get; private set; }
    Rigidbody2D rigid;
    Transform owner;
    bool launched;
    readonly RaycastHit2D[] hits = new RaycastHit2D[32];

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        rigid.gravityScale = 0f;
        rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    void OnEnable()
    {
        HasHit = false;
        launched = false;
        Destroy(gameObject, 10f); // Also clean up an unlaunched instance.
    }

    public void Fire(Vector3 target)
    {
        Fire(target, 5f, 30f, 3f, null);
    }

    public void Fire(Vector3 target, float damage, float speed, float lifetime, Transform source)
    {
        owner = source;
        Damage = Mathf.Max(0f, damage);
        Vector2 direction = target - transform.position;
        if (direction.sqrMagnitude < 0.0001f) direction = Vector2.right;
        direction.Normalize();
        transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 180f);
        rigid.velocity = direction * Mathf.Max(0.1f, speed);
        launched = true;
        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    void FixedUpdate()
    {
        if (!launched || HasHit || StageRewardController.InputBlocked) return;
        // Sweep the arrow's collider so a fast arrow cannot skip thin/overlapping hitboxes.
        Vector2 travel = rigid.velocity * Time.fixedDeltaTime;
        if (travel.sqrMagnitude <= 0f) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        filter.useTriggers = true;
        int count = rigid.Cast(travel.normalized, filter, hits, travel.magnitude);
        for (int i = 0; i < count; i++)
        {
            if (TryHit(hits[i].collider)) break;
        }
    }

    void OnTriggerEnter2D(Collider2D other) { TryHit(other); }
    void OnCollisionEnter2D(Collision2D other) { TryHit(other.collider); }

    public bool TryHit(Collider2D other)
    {
        if (HasHit || !launched || other == null || StageRewardController.InputBlocked) return false;
        if (other.transform.IsChildOf(transform) || (owner != null && other.transform.IsChildOf(owner)) ||
            other.GetComponentInParent<Player>() != null || other.GetComponentInParent<Arrow>() != null) return false;
        EnemyData enemy = other.GetComponentInParent<EnemyData>();
        if (enemy == null && (other.isTrigger || other.attachedRigidbody != null)) return false;

        // Consume BEFORE damage callbacks. All subsequent hitboxes are ignored, even this physics step.
        HasHit = true;
        launched = false;
        rigid.velocity = Vector2.zero;
        foreach (Collider2D hitbox in GetComponentsInChildren<Collider2D>()) hitbox.enabled = false;
        rigid.simulated = false;
        if (enemy != null)
        {
            var explosive = GetComponent<BowExplosiveArrowImpact>();
            if (explosive != null) explosive.Explode(enemy, transform.position, Damage);
            else
            {
                enemy.TakeDamage(Damage, CombatDamageKind.Normal);
                enemy.Hitted();
            }
        }
        Destroy(gameObject);
        return true;
    }
}
