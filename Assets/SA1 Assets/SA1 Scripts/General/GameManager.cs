using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public GameObject playerObject;
    public GameObject[] enemies;

    [Header("SCENE MANAGEMENT")]
    public string mainMenuName = "MainMenu";
    public string level1Name = "Level1";
    public string level2Name = "Level2";
    public string creditsSceneName = "Credits";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += HandleGlobalSceneSetup;
    }

    private void HandleGlobalSceneSetup(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[GAMEMANAGER] Scene Loaded: {scene.name}");

        if (scene.name.StartsWith("Level") || scene.name.Contains("Level"))
        {
            playerObject = GameObject.FindWithTag("Player");
            enemies = GameObject.FindGameObjectsWithTag("Enemy");
            Debug.Log($"[GAMEMANAGER] References gathered. Player found: {playerObject != null}, Enemies found: {enemies?.Length}");

            // Auto-bind Main Menu / Exit Button inside gameplay levels
            GameObject pauseMenuBtnObj = GameObject.Find("MainMenuBtn");

            if (pauseMenuBtnObj != null)
            {
                UnityEngine.UI.Button btn = pauseMenuBtnObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(SavePlayer);
                    btn.onClick.AddListener(SaveEnemies);
                    btn.onClick.AddListener(() => {
                        Debug.Log("[GAMEMANAGER] Leaving level. Loading Main Menu scene...");
                        SceneManager.LoadScene(mainMenuName);
                    });
                    Debug.Log("[GAMEMANAGER] Successfully auto-bound the Pause Menu 'Main Menu' button code-side!");
                }
            }
            return;
        }

        if (scene.name == mainMenuName)
        {
            GameObject loadButtonObj = GameObject.Find("LoadGamebtn");

            if (loadButtonObj != null)
            {
                UnityEngine.UI.Button btn = loadButtonObj.GetComponent<UnityEngine.UI.Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(OnClickLoadFromMenu);
                    Debug.Log("[GAMEMANAGER] Successfully auto-bound the LoadGamebtn code-side!");
                }
            }
        }
    }

    public void SavePlayer()
    {
        if (playerObject != null)
        {
            SaveSystem.SavePlayer(playerObject.GetComponent<ShootHandler>(), playerObject.GetComponent<HealthHandler>());
            Debug.Log("[GAMEMANAGER] Player data saved.");
        }
    }

    public void SaveEnemies()
    {
        if (enemies != null && enemies.Length > 0)
        {
            SaveSystem.SaveEnemy(enemies);
            Debug.Log("[GAMEMANAGER] Enemy data saved.");
        }
    }

    public void LoadPlayer()
    {
        PlayerData data = SaveSystem.LoadPlayer();
        if (data != null && playerObject != null)
        {
            playerObject.GetComponent<ShootHandler>().SetCurrentAmmo(data.currentAmmo);
            playerObject.GetComponent<ShootHandler>().SetTotalAmmo(data.totalAmmo);
            playerObject.GetComponent<HealthHandler>().SetHealth(data.health);

            // Restored full position array index pointers safely
            playerObject.transform.position = new Vector3(data.position[0], data.position[1], data.position[2]);
            Debug.Log("[GAMEMANAGER] Player stats and position successfully applied.");
        }
    }

    public void LoadEnemies()
    {
        int i = 0;
        foreach (var enemy in enemies)
        {
            EnemySaveData data = SaveSystem.LoadEnemy();

            if (data != null && data.enemies != null && enemy != null && i < data.enemies.Count)
            {
                enemy.SetActive(data.enemies[i].activeInScene);
                enemy.GetComponent<HealthHandler>().SetHealth(data.enemies[i].health);

                // Restored full enemy position array index pointers safely
                enemy.transform.position = new Vector3(data.enemies[i].position[0], data.enemies[i].position[1], data.enemies[i].position[2]);
            }
            i++;
        }
        Debug.Log("[GAMEMANAGER] Enemy array status and positions applied.");
    }

    public void OnClickLoadFromMenu()
    {
        Debug.Log("[GAMEMANAGER] Load button was physically clicked!");

        // FIXED: Now looking for the correct binary file path matching your SaveSystem!
        string playerSavePath = System.IO.Path.Combine(Application.persistentDataPath, "PlayerData.dat");
        Debug.Log($"[GAMEMANAGER] Looking for save file at: {playerSavePath}");

        if (!System.IO.File.Exists(playerSavePath))
        {
            Debug.LogError("[GAMEMANAGER] Aborting Load: No save file found on disk!");
            return;
        }

        PlayerData data = SaveSystem.LoadPlayer();
        if (data != null)
        {
            string sceneToLoad = data.sceneName;
            Debug.Log($"[GAMEMANAGER] Save file found! Scene inside save file is: '{sceneToLoad}'");

            if (!string.IsNullOrEmpty(sceneToLoad) && sceneToLoad != creditsSceneName)
            {
                SceneManager.sceneLoaded += ApplyLoadedDataOnSceneComplete;
                SceneManager.LoadScene(sceneToLoad);
                Debug.Log($"[GAMEMANAGER] Loading scene: {sceneToLoad}");
            }
        }
    }

    private void ApplyLoadedDataOnSceneComplete(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= ApplyLoadedDataOnSceneComplete;

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Debug.Log($"[GAMEMANAGER] Scene {scene.name} is now live. Initializing asset injections...");

        playerObject = GameObject.FindWithTag("Player");
        enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (playerObject != null)
        {
            LoadPlayer();

            // --- FIX: Force UI Manager updates if it exists in the level ---
            // This grabs the active UIManager script in your scene and tells it to refresh the health slider and text displays
            object uiMgr = GameObject.FindAnyObjectByType(Type.GetType("UIManager"));
            if (uiMgr != null)
            {
                // Forces your UI system to manually poll current stats from health and weapon scripts
                uiMgr.GetType().GetMethod("UpdateUI")?.Invoke(uiMgr, null);
                uiMgr.GetType().GetMethod("UpdateHUD")?.Invoke(uiMgr, null);
                Debug.Log("[GAMEMANAGER] Successfully requested HUD interface refresh from UI Manager assets.");
            }
        }

        if (enemies != null && enemies.Length > 0)
        {
            LoadEnemies();
        }
    }


    public void CheckLevelStatus()
    {
        int aliveEnemies = 0;
        if (enemies != null)
        {
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
        }

        if (aliveEnemies == 0)
        {
            HandleSceneTransition();
        }
    }

    private void HandleSceneTransition()
    {
        SavePlayer();
        SaveEnemies();

        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == level1Name) SceneManager.LoadScene(level2Name);
        else if (currentScene == level2Name)
        {
            DeleteSaveFiles();
            SceneManager.LoadScene(creditsSceneName);
        }
    }

    private void DeleteSaveFiles()
    {
        try
        {
            // FIXED: Wipes matching binary save files upon assignment completion
            string playerSavePath = System.IO.Path.Combine(Application.persistentDataPath, "PlayerData.dat");
            string enemySavePath = System.IO.Path.Combine(Application.persistentDataPath, "EnemyData.dat");

            if (System.IO.File.Exists(playerSavePath)) System.IO.File.Delete(playerSavePath);
            if (System.IO.File.Exists(enemySavePath)) System.IO.File.Delete(enemySavePath);
            Debug.Log("[GAMEMANAGER] Vertical slice cleared: Saves deleted.");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cleaning up saves: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleGlobalSceneSetup;
    }
}
