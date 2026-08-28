using System.Collections;
using UnityEngine;

public class Ghost : MonoBehaviour
{
    [Header("Components")]
    EnemyMove enemyMove;    
    Animator animator;
    EnemyData enemyData;
    SpriteRenderer sr;

    [Header("Player References")]
    GameObject player;
    Transform playerTransform;
    PlayerMove playerMove;
    Player playerScript;

    [Header("Attack Settings")]
    [SerializeField] bool attackAble = true;
    [SerializeField] float attackCooltime = 1.0f;
    [SerializeField] float attackRange = 1.5f; 

    [Header("Stealth Settings")]
    [SerializeField] bool stealthAble = true;
    [SerializeField] bool isStealthed = false;
    [SerializeField] float stealthCooldown = 8f;
    [SerializeField] float stealthDuration = 3f;
    [SerializeField] float teleportDistance = 3f;
    [SerializeField] float invisibilityDuration = 1f;
    [SerializeField] float stealthAlpha = 0.3f;
    [SerializeField] float teleportBehindChance = 0.3f;
    [SerializeField] float attackDelayAfterTeleport = 0.5f;

    [Header("Audio Settings")]
    [SerializeField] AudioClip stealthSound;
    [SerializeField] AudioClip teleportSound;
    [SerializeField] AudioClip attackSound;
    
    [Header("Visual Effects")]
    [SerializeField] GameObject teleportEffect;
    [SerializeField] GameObject attackEffect;

    Coroutine stealthCoroutine;
    AudioSource audioSource;
    bool isDead = false;
    bool justTeleported = false;

    void Start()
    {
        InitializeComponents();
        SetupGhostProperties();
        StartStealthBehavior();
    }

    void InitializeComponents()
    {
        enemyMove = GetComponent<EnemyMove>();
        animator = GetComponentInChildren<Animator>();
        enemyData = GetComponent<EnemyData>();
        sr = GetComponentInChildren<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.5f;
        }

        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerMove = player.GetComponent<PlayerMove>();
            playerScript = player.GetComponent<Player>();
        }
        else
        {
            Debug.LogError("Player not found! Make sure Player GameObject has 'Player' tag.");
        }
    }

    void SetupGhostProperties()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) 
        {
            col.isTrigger = true;
        }
    }

    void StartStealthBehavior()
    {
        if (stealthCoroutine == null && !isDead)
        {
            stealthCoroutine = StartCoroutine(StealthRoutine());
        }
    }

    void Update()
    {
        HandleDeath();
    }

    void HandleDeath()
    {
        if (enemyData.enemy_current_HP <= 0 && !isDead)
        {
            isDead = true;
            justTeleported = false;
            
            if (stealthCoroutine != null)
            {
                StopCoroutine(stealthCoroutine);
                stealthCoroutine = null;
            }
            
            animator.SetTrigger("Death");
            enemyMove.moveable = false;
            
            if (isStealthed)
            {
                RevealGhost();
            }
            
            Destroy(gameObject, 1f);
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (ShouldAttackPlayer(other))
        {
            PerformNormalAttack();
        }
    }

    bool ShouldAttackPlayer(Collider2D other)
    {
        if (!other.CompareTag("Player") || !attackAble || isStealthed || 
            playerScript == null || enemyData.enemy_current_HP <= 0 || justTeleported)
            return false;

        float distance = Vector2.Distance(transform.position, other.transform.position);
        return distance <= attackRange;
    }

    void PerformNormalAttack()
    {
        animator.SetInteger("Attack", 0);
        PlaySound(attackSound);
        ShowEffect(attackEffect);

        DamagePlayer(enemyData.enemy_power);
        CameraShake(); // ✅ 공격 시 카메라 흔들림

        attackAble = false;
        Invoke(nameof(ResetAttack), attackCooltime);
    }

    void DamagePlayer(float damage)
    {
        if (playerScript != null)
        {
            GameManager.player_current_HP -= damage;
            playerScript.Hited();
        }
    }

    void ResetAttack()
    {
        attackAble = true;
        animator.SetInteger("Attack", -1);
    }

    IEnumerator StealthRoutine()
    {
        while (enemyData.enemy_current_HP > 0 && !isDead)
        {
            yield return new WaitForSeconds(stealthCooldown);

            if (stealthAble && playerTransform != null && !isDead)
            {
                yield return StartCoroutine(ExecuteStealthSequence());
            }

            yield return null;
        }
    }

    IEnumerator ExecuteStealthSequence()
    {
        EnterStealth();
        yield return new WaitForSeconds(stealthDuration);
        
        BecomeInvisible();
        yield return new WaitForSeconds(invisibilityDuration);
        
        justTeleported = true;
        
        TeleportNearPlayer();
        RevealGhost();
        
        yield return new WaitForSeconds(attackDelayAfterTeleport);
        
        justTeleported = false;
    }

    void EnterStealth()
    {
        stealthAble = false;
        isStealthed = true;
        
        PlaySound(stealthSound);
        
        if (sr != null) 
        {
            Color currentColor = sr.color;
            sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, stealthAlpha);
        }
        
        animator.SetBool("Stealth", true);
    }

    void RevealGhost()
    {
        isStealthed = false;
        
        PlaySound(teleportSound);
        ShowEffect(teleportEffect);
        
        if (sr != null) 
        {
            Color currentColor = sr.color;
            sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, 1f);
        }
        
        animator.SetBool("Stealth", false);
        stealthAble = true;
    }

    void BecomeInvisible()
    {
        if (sr != null) 
        {
            Color currentColor = sr.color;
            sr.color = new Color(currentColor.r, currentColor.g, currentColor.b, 0f);
        }
    }

    void TeleportNearPlayer()
    {
        if (playerTransform == null || playerMove == null) return;

        Vector3 targetPosition;
        bool teleportBehind = Random.value < teleportBehindChance;
        
        if (teleportBehind)
        {
            Vector3 offset = playerMove.player_direction == 0 ? 
                Vector3.right * teleportDistance : Vector3.left * teleportDistance;
            targetPosition = playerTransform.position + offset;
        }
        else
        {
            Vector3 offset = playerMove.player_direction == 0 ? 
                Vector3.left * teleportDistance : Vector3.right * teleportDistance;
            targetPosition = playerTransform.position + offset;
        }

        targetPosition = ValidateTeleportPosition(targetPosition);
        transform.position = targetPosition;
    }

    Vector3 ValidateTeleportPosition(Vector3 targetPos)
    {
        return targetPos;
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void ShowEffect(GameObject effect)
    {
        if (effect != null)
        {
            GameObject fx = Instantiate(effect, transform.position, Quaternion.identity);
            Destroy(fx, 2f);
        }
    }

    void CameraShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            StartCoroutine(Shake(mainCamera.transform, 0.15f, 0.3f));
        }
    }

    IEnumerator Shake(Transform cameraTransform, float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            cameraTransform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }

    void OnDestroy()
    {
        if (stealthCoroutine != null)
        {
            StopCoroutine(stealthCoroutine);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        if (playerTransform != null && playerMove != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 frontPos, backPos;
            
            if (playerMove.player_direction == 0) 
            {
                frontPos = playerTransform.position + Vector3.left * teleportDistance;
                backPos = playerTransform.position + Vector3.right * teleportDistance;
            }
            else 
            {
                frontPos = playerTransform.position + Vector3.right * teleportDistance;
                backPos = playerTransform.position + Vector3.left * teleportDistance;
            }
            
            Gizmos.DrawWireSphere(frontPos, 0.5f);
            Gizmos.DrawLine(playerTransform.position, frontPos);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(backPos, 0.5f);
            Gizmos.DrawLine(playerTransform.position, backPos);
        }
    }
}
