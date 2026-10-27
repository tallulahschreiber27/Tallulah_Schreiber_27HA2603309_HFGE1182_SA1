using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // <-- ADDED TO HANDLE SCENE TRANSITIONS

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

    private void UpdateAmmoUI()
    {
        if (shootHandler == null || txtAmmo == null)
            return;

        txtAmmo.text =
            $"{shootHandler.GetCurrentAmmo()} / {shootHandler.GetTotalAmmo()}";
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
    // --- ADDED NAVIGATION METHODS FOR PAUSE MENU & MAIN MENU ---
    // =================================================================

    /// <summary>
    /// Call this from your Pause Menu's "Main Menu" button.
    /// </summary>
    public void GoToMainMenu()
    {
        Time.timeScale = 1f; // Unfreeze time before loading, or the menu will be frozen!
        SceneManager.LoadScene("MainMenu"); // Make sure your main menu scene is named exactly this!
    }

    /// <summary>
    /// Call this from your Main Menu's "Start Game" button.
    /// </summary>
    public void PlayGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1"); // Make sure your first gameplay level scene is named exactly this!
    }

    public void QuitGame()
    {
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
