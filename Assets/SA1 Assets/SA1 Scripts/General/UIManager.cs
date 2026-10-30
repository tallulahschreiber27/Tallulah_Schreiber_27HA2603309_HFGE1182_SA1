using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public GameObject hud;
    public GameObject pauseMenu;

    public TextMeshProUGUI txtAmmo;
    public ShootHandler shootHandler;

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
        UpdateUI(); // Force a fresh UI draw on start
    }

    private void Update()
    {
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

    /// <summary>
    /// Added to match the GameManager's cross-scene loading refresh call perfectly!
    /// </summary>
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

        // Calls the secure getters inside your ShootHandler script
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
    // --- NAVIGATION METHODS FOR PAUSE MENU & MAIN MENU ---
    // =================================================================

    /// <summary>
    /// Connect this method directly to your Main Menu "Load Game" Button OnClick() event handler!
    /// </summary>
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
