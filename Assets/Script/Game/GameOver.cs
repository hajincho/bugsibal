using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOver : MonoBehaviour
{
    public GameObject restartUI;
    private bool isPaused = false;

    void Start()
    {
        if (restartUI != null)
            restartUI.SetActive(false);
        else
            Debug.LogWarning("restartUI가 연결되지 않았습니다.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        if (isPaused && Input.GetKeyDown(KeyCode.Return))
        {
            RestartScene();
        }
    }

    void TogglePause()
    {
        isPaused = !isPaused;

        if (restartUI != null)
            restartUI.SetActive(isPaused);

        Time.timeScale = isPaused ? 0f : 1f;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
