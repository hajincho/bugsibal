using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverBG : MonoBehaviour
{
    public GameObject gameoverUI;
    public GameObject button;

    GameObject player;
    GameObject spawn;

    private bool gameOverTriggered = false;

    void Start()
    {
        gameoverUI.SetActive(false);
        button.SetActive(false);

        player = GameObject.FindGameObjectWithTag("Player");
        spawn = GameObject.FindGameObjectWithTag("Spawn");
    }

    void Update()
    {
        if (GameManager.player_current_HP <= 0 && !gameOverTriggered)
        {
            TriggerGameOver();
        }

        if (gameOverTriggered && Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Restart();
        }
    }

    void TriggerGameOver()
    {
        gameOverTriggered = true;
        gameoverUI.SetActive(true);
        button.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Restart()
    {
        gameOverTriggered = false;
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        gameoverUI.SetActive(false);
        button.SetActive(false);

        player = GameObject.FindGameObjectWithTag("Player");
        spawn = GameObject.FindGameObjectWithTag("Spawn");


        if (player != null && spawn != null)
            player.transform.position = spawn.transform.position;

        GameManager.player_current_HP = GameManager.player_HP;

        gameOverTriggered = false;
        Time.timeScale = 1f;
    }
}
