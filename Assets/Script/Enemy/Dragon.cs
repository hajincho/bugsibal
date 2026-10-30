using System.Collections;
using UnityEngine;

// 평소엔 작은 용(Dragon_Idle)으로 쫓아오다가 사거리에 들어오면 멈춰서, 1.5배로 커지며 몸 가로길이의 5배 길이로 플레이어 쪽에 불을 뿜는다.
// 모습은 Animator의 Dragon_Idle / Dragon_Attack 클립이 담당하므로 새 모션 프레임은 클립만 교체하면 된다.
// 원본 그림은 왼쪽을 보고 있으므로 프리팹 루트의 scale.x는 -1이다(EnemyMove가 플레이어 쪽으로 뒤집음).
public class Dragon : MonoBehaviour
{
    public Transform fire;                      // 입 위치에 둔 불길. 스프라이트 피벗이 왼쪽 가운데(=입)
    public float fireLengthMultiplier = 5f;     // 몸 가로길이 대비 불길 길이
    public float fireWidth = 1.6f;              // 불길 끝 폭(입 쪽은 0.6)
    public float windup = 0.5f;                 // 공격 모습으로 바뀌며 커진 뒤 불을 뿜기까지
    public float attackScale = 1.5f;            // 공격할 때 커지는 배율(평소 크기는 그대로)
    public float shrinkTime = 0.25f;            // 불을 다 뿜고 원래 크기로 돌아오는 시간
    public float breathDuration = 1.5f;
    public float tickInterval = 0.3f;           // 불길 안에 있으면 이 간격마다 피해
    public float cooltime = 3f;
    public int dropchance = 30;

    EnemyMove enemyMove;
    Animator animator;
    EnemyData enemyData;
    ItemDrop itemdrop;
    Transform player;
    Collider2D playerCollider;
    Player playerScript;
    float fireLength, originX, originY, facing, nextAttack;
    Vector3 fireBaseScale;
    bool breathing, isdead;
    Vector2 aim;

    void Start()
    {
        enemyMove = GetComponent<EnemyMove>();
        animator = GetComponent<Animator>();
        enemyData = GetComponent<EnemyData>();
        itemdrop = GetComponent<ItemDrop>();
        var p = GameObject.FindGameObjectWithTag("Player");
        player = p.transform;
        playerCollider = p.GetComponent<Collider2D>();
        playerScript = p.GetComponent<Player>();
        originX = transform.localScale.x;
        originY = transform.localScale.y;

        // 몸 가로길이는 평소(작은 용) 모습 기준. Start 시점의 Renderer.bounds는 갱신 전일 수 있어 스프라이트 크기로 계산
        fireLength = GetComponent<SpriteRenderer>().sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x) * fireLengthMultiplier;
        var fireSprite = fire.GetComponent<SpriteRenderer>().sprite.bounds.size;
        fireBaseScale = new Vector3(fireLength / fireSprite.x, fireWidth / fireSprite.y, 1f);
        fire.localScale = fireBaseScale;
        fire.gameObject.SetActive(false);
        nextAttack = Time.time + 1f;
    }

    void Update()
    {
        if (enemyData.enemy_current_HP <= 0)
        {
            if (!isdead) Die();
            return;
        }
        if (EnemyFrostStatus.IsFrozenOn(this)) return;
        if (!breathing && Time.time >= nextAttack && Vector2.Distance(PlayerPoint(), fire.position) <= fireLength * 0.9f)
            StartCoroutine(Breathe());
    }

    IEnumerator Breathe()
    {
        breathing = true;
        enemyMove.moveable = false;
        animator.SetBool("Breathing", true);

        // 플레이어 쪽으로 몸을 돌리고 커진 뒤 입 위치에서 조준. 뿜는 동안 방향은 고정
        facing = PlayerPoint().x >= transform.position.x ? originX : -originX;
        yield return Resize(1f, attackScale, windup);
        aim = (PlayerPoint() - (Vector2)fire.position).normalized;
        if (aim == Vector2.zero) aim = Vector2.right * Mathf.Sign(-transform.localScale.x);
        Vector3 local = transform.InverseTransformVector(aim); // 뒤집힌 루트 기준 방향
        fire.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        fire.gameObject.SetActive(true);

        float end = Time.time + breathDuration;
        while (Time.time < end)
        {
            if (!EnemyFrostStatus.IsFrozenOn(this) && InFire())
            {
                GameManager.DamagePlayer(enemyData.enemy_power);
                playerScript.Hited();
            }
            yield return new WaitForSeconds(tickInterval);
        }
        fire.gameObject.SetActive(false);
        yield return Resize(attackScale, 1f, shrinkTime);
        StopBreath();
    }

    IEnumerator Resize(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            SetSize(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration)));
            yield return null;
        }
        SetSize(to);
    }

    // 몸만 커지고 불길 길이·폭은 그대로 유지(불길은 자식이라 역으로 줄임)
    void SetSize(float size)
    {
        transform.localScale = new Vector3(facing * size, originY * size, transform.localScale.z);
        fire.localScale = fireBaseScale / size;
    }

    // 꺼지면 코루틴이 끊기므로 불길/상태가 켜진 채로 남지 않게 정리
    void OnDisable() { if (breathing && !isdead) StopBreath(); }

    void StopBreath()
    {
        SetSize(1f);
        fire.gameObject.SetActive(false);
        animator.SetBool("Breathing", false);
        enemyMove.moveable = true;
        breathing = false;
        nextAttack = Time.time + cooltime;
    }

    // 입에서 aim 방향으로 길이 fireLength, 폭이 0.6 → fireWidth로 넓어지는 원뿔 안인지
    bool InFire()
    {
        Vector2 d = PlayerPoint() - (Vector2)fire.position;
        float along = Vector2.Dot(d, aim);
        if (along < 0f || along > fireLength) return false;
        float half = Mathf.Lerp(0.3f, fireWidth / 2f, along / fireLength);
        return Mathf.Abs(d.x * aim.y - d.y * aim.x) <= half;
    }

    Vector2 PlayerPoint() => playerCollider != null ? (Vector2)playerCollider.bounds.center : (Vector2)player.position;

    void Die()
    {
        isdead = true;
        StopAllCoroutines();
        if (breathing) SetSize(1f);
        fire.gameObject.SetActive(false);
        enemyMove.moveable = false;
        if (itemdrop != null && Random.Range(0, 10) <= (dropchance - 10) / 10) itemdrop.AddItemInventory(transform.position);
        Destroy(gameObject, 0.5f);
    }
}
