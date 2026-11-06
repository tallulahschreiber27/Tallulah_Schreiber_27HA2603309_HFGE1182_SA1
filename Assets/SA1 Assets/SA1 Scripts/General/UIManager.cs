using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    [Header("MENUS & PANELS")]
    public GameObject hud;
    public GameObject pauseMenu;
    public GameObject gameOverPanel; // SA1 Custom: Slot for your Game Over Panel!

    [Header("SHOOT INTEGRATION")]
    public TextMeshProUGUI txtAmmo;
    public ShootHandler shootHandler;

    [Header("HEALTH INTEGRATION")]
    public Slider sldHealth;
    public HealthHandler healthHandler;

    public static UIManager Instance { get; private set; }

    public bool IsPaused { get; private set; }

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

    private void Start()
    {
        ResumeGame();

        // Ensure game over screen is hidden when starting up
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        UpdateUI(); // Force a fresh UI draw on start
    }

    private void Update()
    {
        // Don't allow pausing if the player is already dead/game over
        if (gameOverPanel != null && gameOverPanel.activeSelf) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused)
                ResumeGame();
            else
                PauseGame();
        }

        // Continually refresh UI status in update loop
        UpdateHealthUI();
        UpdateAmmoUI();
    }

    public void UpdateUI()
    {
        UpdateHealthUI();
        UpdateAmmoUI();
    }

    public void UpdateHealthUI()
    {
        if (healthHandler == null || sldHealth == null)
            return;

        sldHealth.maxValue = healthHandler.GetMaxHealth();
        sldHealth.value = healthHandler.GetHealth();
    }

    public void UpdateAmmoUI()
    {
        if (shootHandler == null || txtAmmo == null)
            return;

        txtAmmo.text = $"{shootHandler.GetCurrentAmmo()} / {shootHandler.GetTotalAmmo()}";
    }

    public void PauseGame()
    {
        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseMenu != null)
            pauseMenu.SetActive(true);

        if (hud != null)
            hud.SetActive(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenu != null)
            pauseMenu.SetActive(false);

        if (hud != null)
            hud.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // =================================================================
    // --- SA1 CUSTOM: GAME OVER PANEL CONTROL METHODS ---
    // =================================================================

    /// <summary>
    /// Call this from HealthHandler when the player dies to pop open the Game Over menu!
    /// </summary>
    public void TriggerGameOver()
    {
        Time.timeScale = 0f; // Freeze game physics

        if (hud != null)
            hud.SetActive(false);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None; // Free the mouse cursor
        Cursor.visible = true;
    }

    /// <summary>
    /// Attach this method directly to your Game Over panel's "Load Last Save" Button OnClick event!
    /// </summary>
    public void HandleGameOverLoadPressed()
    {
        Time.timeScale = 1f; // Unfreeze time scale before loading!

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        // Uses your lecturer's exact custom loading procedure
        if (GameManager.Instance != null)
        {
            Debug.Log("[UI MANAGER] Game Over: Loading last valid player save sequence...");
            GameManager.Instance.OnClickLoadFromMenu();
        }
        else
        {
            Debug.LogError("[UI MANAGER] CRITICAL: No active GameManager instance found to process save loading!");
        }
    }

    // =================================================================
    // --- NAVIGATION METHODS FOR PAUSE MENU & MAIN MENU ---
    // =================================================================

    public void HandleLoadButtonPressed()
    {
        GameManager activeManager = FindObjectOfType<GameManager>();

        if (activeManager != null)
        {
            Debug.Log("[UI MANAGER] Successfully located active GameManager. Triggering loading routine...");
            activeManager.OnClickLoadFromMenu();
        }
        else
        {
            if (GameManager.Instance != null)
            {
                Debug.Log("[UI MANAGER] Located GameManager via static Instance wrapper. Triggering loading routine...");
                GameManager.Instance.OnClickLoadFromMenu();
            }
            else
            {
                Debug.LogError("[UI MANAGER] CRITICAL: No active GameManager discovered to link save files!");
            }
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
