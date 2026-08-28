using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropSword : MonoBehaviour
{
    Ultimate ultimate;
    Transform transform;

    public GameObject effect;
    private bool isFalling = false;
    private float fallSpeed = 45f;
    private float fallDuration = 0.1f;
    private float fallStartTime;
    void OnEnable()
    {
        transform = GetComponent<Transform>();
        ultimate = GameObject.FindWithTag("Player").GetComponent<Ultimate>();
        Destroy(gameObject, 2f);
        isFalling = true;
        fallStartTime = Time.time;
        Invoke("SpawnEffect", 0.15f);
    }

    void Update()
    {
        if (isFalling)
        {
            if (Time.time < fallStartTime + fallDuration)
            {
                transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            }
            else
            {
                isFalling = false;
            }

        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            EnemyData enemy_data = other.GetComponent<EnemyData>();
            IsSkillDamaged skilldamaged = other.GetComponent<IsSkillDamaged>();
            if (enemy_data == null)
            {
                enemy_data = other.GetComponentInParent<EnemyData>();
            }

            if (skilldamaged == null)
            {
                skilldamaged = other.GetComponentInParent<IsSkillDamaged>();
            }

            if (!skilldamaged.ult1)
            {
                enemy_data.enemy_current_HP -= GameManager.player_skill_power * 3 + GameManager.player_power * 3;
                enemy_data.Hitted();
                skilldamaged.ult1 = true;
                ultimate.SkillDamageEnable(skilldamaged, 6);
            }
        }
    }
    void SpawnEffect()
    {
        //Instantiate(effect , transform.position + new Vector3(0, -5, 0), Quaternion.identity);
    }

    void CameraShake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            StartCoroutine(Shake(mainCamera.transform, 0.04f, 0.01f));
        }
    }

    IEnumerator Shake(Transform cameraTransform, float duration, float magnitude)
    {
        Vector3 originalPos = cameraTransform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-0.01f, 0.01f) * magnitude;
            float y = Random.Range(-0.01f, 0.01f) * magnitude;

            cameraTransform.localPosition = new Vector3(x, y, originalPos.z);
            elapsed += Time.deltaTime;

            yield return null;
        }

        cameraTransform.localPosition = originalPos;
    }
}
