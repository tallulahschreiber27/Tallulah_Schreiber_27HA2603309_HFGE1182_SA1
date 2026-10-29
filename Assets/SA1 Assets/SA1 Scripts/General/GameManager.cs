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
        // Singleton pattern to ensure the manager survives across scene updates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps the GameManager alive between scenes so it can run the loading sequence
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

        // Restored vector array indexes completely
        playerObject.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
    }

    public void LoadEnemies()
    {
        int i = 0;
        foreach (var enemy in enemies)
        {
            EnemySaveData data = SaveSystem.LoadEnemy();

            // Safety check: Make sure data exists before indexing into it
            if (data != null && data.enemies != null && enemy != null)
            {
                enemy.SetActive(data.enemies[i].activeInScene);
                enemy.GetComponent<HealthHandler>().SetHealth(data.enemies[i].health);

                // Restored your exact array indexes for enemy positions
                enemy.transform.position = new Vector3(data.enemies[i].position[0], data.enemies[i].position[1], data.enemies[i].position[2]);
            }
            i++;
        }
    }


    // =================================================================
    // --- ADVANCED CROSS-SCENE LOADING SYSTEM (SECTION B & C) ---
    // =================================================================

    /// <summary>
    /// Connect this method directly to your Main Menu "Load Button" OnClick() event!
    /// </summary>
    public void OnClickLoadFromMenu()
    {
        Debug.Log("[GAMEMANAGER] Load button clicked.");

        // Section C Safety Check: Verify if the save file path actually exists on the disk first
        string playerSavePath = System.IO.Path.Combine(Application.persistentDataPath, "player.json"); // Match your exact file name

        if (!System.IO.File.Exists(playerSavePath))
        {
            Debug.LogWarning("[GAMEMANAGER] Aborting Load: No physical save file exists on disk yet!");
            // Optional: Trigger a UI popup here saying "No save game found!"
            return;
        }

        PlayerData data = SaveSystem.LoadPlayer();

        if (data != null)
        {
            string sceneToLoad = data.sceneName;

            // Ensure the saved scene name isn't blank, and don't let it load straight into Credits from the menu
            if (!string.IsNullOrEmpty(sceneToLoad) && sceneToLoad != creditsSceneName)
            {
                SceneManager.sceneLoaded += ApplyLoadedDataOnSceneComplete;
                SceneManager.LoadScene(sceneToLoad);
            }
            else
            {
                Debug.LogWarning($"[GAMEMANAGER] Save file contains invalid scene target: '{sceneToLoad}'. Aborting load transition.");
            }
        }
    }


    /// <summary>
    /// Executes automatically the precise frame the new level geometry becomes live.
    /// </summary>
    private void ApplyLoadedDataOnSceneComplete(Scene scene, LoadSceneMode mode)
    {
        // Unsubscribe immediately so it doesn't loop on future transitions
        SceneManager.sceneLoaded -= ApplyLoadedDataOnSceneComplete;

        Debug.Log($"Loaded into {scene.name}. Dynamically binding references and executing loads...");

        // Re-find references in the freshly loaded scene to avoid null references
        playerObject = GameObject.FindWithTag("Player");
        enemies = GameObject.FindGameObjectsWithTag("Enemy");

        // Fire the load data applications safely
        if (playerObject != null)
        {
            LoadPlayer();
        }

        if (enemies != null && enemies.Length > 0)
        {
            LoadEnemies();
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

        if (currentScene == level1Name)
        {
            SceneManager.LoadScene(level2Name);
        }
        else if (currentScene == level2Name)
        {
            // Section C requirement: "The save file must be deleted once the vertical slice is completed."
            // We call a deletion file sweep right before moving into the credits.
            DeleteSaveFiles();
            SceneManager.LoadScene(creditsSceneName);
        }
        else
        {
            Debug.LogWarning($"Scene transition triggered from unrecognized scene: {currentScene}");
        }
    }

    /// <summary>
    /// Cleans up save data files upon completing the vertical slice assignment requirements.
    /// </summary>
    private void DeleteSaveFiles()
    {
        try
        {
            // We assume SaveSystem has a method or you can use standard System.IO file deletions
            string playerSavePath = System.IO.Path.Combine(Application.persistentDataPath, "player.json"); // Change file name if different
            string enemySavePath = System.IO.Path.Combine(Application.persistentDataPath, "enemy.json");

            if (System.IO.File.Exists(playerSavePath)) System.IO.File.Delete(playerSavePath);
            if (System.IO.File.Exists(enemySavePath)) System.IO.File.Delete(enemySavePath);

            Debug.Log("Vertical slice complete: Save data wiped successfully.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cleaning up vertical slice save tracks: {e.Message}");
        }
    }
}
