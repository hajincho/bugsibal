using System.Collections;
using UnityEngine;

public class ThrownSword : MonoBehaviour
{
    public float speed = 90f;
    public float rotationDuration = 0.15f;
    bool damageable = true;

    private bool isFlying = false; 
    void OnEnable()
    {
        isFlying = false;
        Destroy(gameObject, 5f);
    }

    void Update()
    {
        if (isFlying)
        {
            transform.position -= transform.up * speed * Time.deltaTime;
        }
    }

    public void Initialize(Vector2 direction)
    {
        StartCoroutine(AnimateAndFly(direction));
    }


    private IEnumerator AnimateAndFly(Vector2 targetDirection)
    {

        float endAngle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg + 90;

        float startAngle = endAngle - 180f;

        transform.rotation = Quaternion.Euler(0f, 0f, startAngle);

        float elapsedTime = 0f;
        while (elapsedTime < rotationDuration)
        {
            float currentAngle = Mathf.LerpAngle(startAngle, endAngle, elapsedTime / rotationDuration);
            transform.rotation = Quaternion.Euler(0f, 0f, currentAngle);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, endAngle);

        isFlying = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            if (damageable)
            {
                GameManager.player_current_HP -= 15;
            }
            damageable = false;
        }
    }

}
