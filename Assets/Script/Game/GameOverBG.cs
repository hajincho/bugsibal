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

    void OnEnable() { SceneManager.sceneLoaded += EnsureSceneInput; }
    void OnDisable() { SceneManager.sceneLoaded -= EnsureSceneInput; }
    void EnsureSceneInput(Scene scene, LoadSceneMode mode) { EnsureInputSystem(); }
    void EnsureInputSystem()
    {
        // Stage6 has no EventSystem. Create one only when the new scene has none.
        if (UnityEngine.EventSystems.EventSystem.current == null)
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
    }
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
        EnsureInputSystem();
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

        if (player != null) player.GetComponent<Player>().ResetWeaponModeAfterRevive();
        GameManager.player_current_HP = GameManager.player_HP;

        gameOverTriggered = false;
        Time.timeScale = 1f;
    }
}
