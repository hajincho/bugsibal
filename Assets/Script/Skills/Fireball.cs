using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FB : MonoBehaviour
{
    //스킬 변수
    Skill1 skill;
    Rigidbody2D rigid;
    Vector3 direction;
    public GameObject explosion;
    public bool damageable = true;
    public int fbnum;

    //파이어볼 궤적 변수
    private Vector3 startPos;
    private Vector3 endPos;
    private float height = 3f;
    private float travelTime = 1f;
    private float elapsed = 0f;
    private Vector3 prevpos;
    private bool isFlying = false;

    void OnEnable()
    {
        damageable = true;
        Destroy(gameObject, 3f);
        skill = GameObject.FindWithTag("Player").GetComponent<Skill1>();
    }

    public void FireBall(Vector3 targetDir)//발사 목표 위치 설정
    {
        startPos = transform.position;
        endPos = startPos + targetDir * 15f;

        elapsed = 0f;
        isFlying = true;
        prevpos = transform.position;
    }

    void Update()
    {
        //파이어볼 이동
        if (!isFlying) return;

        elapsed += Time.deltaTime;
        float t = elapsed / travelTime;
        if (t > 1f)
        {
            t = 1f;
            isFlying = false;
            Destroy(gameObject);
            Instantiate(explosion, this.transform.position, Quaternion.identity);

        }

        Vector3 linear = Vector3.Lerp(startPos, endPos, t);

        float arc = 0f;
        switch (fbnum)
        {
            case 0: arc = Mathf.Sin(t * Mathf.PI) * height; break;
            case 1: arc = 0f; break;                             
            case 2: arc = -Mathf.Sin(t * Mathf.PI) * height; break; 
        }
        linear.y += arc;

        Vector3 delta = linear - prevpos;

        if (delta.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; 
            transform.rotation = Quaternion.Euler(0f, 0f, angle); 
        }

        transform.position = linear; 
        prevpos = linear; 
    }

    void OnTriggerEnter2D(Collider2D other)//충돌시 피해
    {
        if (other.tag == "Enemy" || other.tag == "drumtong")
        {
            Destroy(gameObject);
            Instantiate(explosion, other.transform.position, Quaternion.identity);
            
            EnemyData enemy_data = other.GetComponent<EnemyData>();
            if (enemy_data == null)
            {
                enemy_data = other.GetComponentInParent<EnemyData>();
            }
            damageable = false;
        }
    }
}