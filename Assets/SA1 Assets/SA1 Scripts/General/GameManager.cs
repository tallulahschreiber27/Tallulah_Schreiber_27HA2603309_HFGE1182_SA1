using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject playerObject;
    public GameObject[] enemies;
    private string sceneName;

    [Header("SCENE MANAGEMENT")]
    [Tooltip("Type the exact name of your first level scene here.")]
    public string level1Name = "Level 1";

    [Tooltip("Type the exact name of your second level scene here.")]
    public string level2Name = "Level 2";

    [Tooltip("Type the exact name of your final credits scene here.")]
    public string creditsSceneName = "Credits";

    private void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SavePlayer()
    {
        SaveSystem.SavePlayer(playerObject.GetComponent<ShootHandler>(), playerObject.GetComponent<HealthHandler>());
    }

    public void SaveEnemies()
    {
        SaveSystem.SaveEnemy(enemies);
    }

    public void LoadPlayer()
    {
        PlayerData data = SaveSystem.LoadPlayer();
        playerObject.GetComponent<ShootHandler>().SetCurrentAmmo(data.currentAmmo);
        playerObject.GetComponent<ShootHandler>().SetTotalAmmo(data.totalAmmo);
        playerObject.GetComponent<HealthHandler>().SetHealth(data.health);
        // Fixed: Restored vector array indexes
        playerObject.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
    }

    public void LoadEnemies()
    {
        int i = 0;
        foreach (var enemy in enemies)
        {
            EnemySaveData data = SaveSystem.LoadEnemy();
            enemy.SetActive(data.enemies[i].activeInScene);
            enemy.GetComponent<HealthHandler>().SetHealth(data.enemies[i].health);
            // Fixed: Restored vector array indexes
            enemy.transform.position = new Vector3(data.enemies[i].position[0], data.enemies[i].position[1], data.enemies[i].position[2]);
            i++;
        }
    }

    // =================================================================
    // --- SYSTEM INTEGRATION METHODS FOR SCENE TRANSITIONS ---
    // =================================================================

    /// <summary>
    /// Scans the level's assigned enemies. If none are alive, triggers the scene transition.
    /// </summary>
    public void CheckLevelStatus()
    {
        int aliveEnemies = 0;

        foreach (var enemy in enemies)
        {
            if (enemy != null)
            {
                HealthHandler healthScript = enemy.GetComponent<HealthHandler>();

                // If the enemy game object exists, is active, and has health remaining, count it
                if (enemy.activeSelf && healthScript != null && healthScript.GetHealth() > 0)
                {
                    aliveEnemies++;
                }
            }
        }

        // Section A Requirement: Level clears by killing the single target enemy
        if (aliveEnemies == 0)
        {
            HandleSceneTransition();
        }
    }

    /// <summary>
    /// Saves all game progress data and automatically moves the player to the next scene.
    /// </summary>
    private void HandleSceneTransition()
    {
        // Section C Requirement: "Saving should occur when transitioning between scenes"
        SavePlayer();
        SaveEnemies();

        string currentScene = SceneManager.GetActiveScene().name;

        // Uses the custom names you type into the Inspector fields above!
        if (currentScene == level1Name)
        {
            SceneManager.LoadScene(level2Name);
        }
        else if (currentScene == level2Name)
        {
            SceneManager.LoadScene(creditsSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene transition triggered from unrecognized scene: {currentScene}");
        }
    }
}
