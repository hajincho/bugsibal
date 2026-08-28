using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordAttack : MonoBehaviour
{
    public SwordMan sword_man;
    public EnemyData enemy_data;

    GameObject player;
    Player playerComponent;

    void Start()
    {
        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerComponent = player.GetComponent<Player>();
        else
            Debug.LogError("Player not found in scene.");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerWeapon") && sword_man.attack_mode == SwordMan.AttackMode.ParryWindow)
        {
            sword_man.Parrying();
        }

        if (other.CompareTag("Player") && playerComponent != null)
        {
            switch (sword_man.attack_mode)
            {
                case SwordMan.AttackMode.Normal:
                    CameraShake();
                    GameManager.player_current_HP -= enemy_data.enemy_power;
                    playerComponent.Hited();
                    break;

                case SwordMan.AttackMode.Down:
                    CameraShake();
                    GameManager.player_current_HP -= enemy_data.enemy_power;
                    playerComponent.Hited();
                    playerComponent.Attacked(1);
                    break;

                case SwordMan.AttackMode.Sting:
                    GameManager.player_current_HP -= (int)(enemy_data.enemy_power / 2f);
                    playerComponent.Hited();
                    playerComponent.Attacked(0);
                    break;

                case SwordMan.AttackMode.ParryWindow:
                    GameManager.player_current_HP -= enemy_data.enemy_power * 2;
                    playerComponent.Hited();
                    playerComponent.Attacked(1);
                    break;
                case SwordMan.AttackMode.Throw:
                    break;
            }
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
}
