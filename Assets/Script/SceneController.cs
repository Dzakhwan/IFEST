using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }

    [Header("UI References (Optional)")]
    [SerializeField] private GameObject pauseMenuPanel;

    public bool IsPaused { get; private set; } = false;

    // Event invoked when pause state changes (true = paused, false = resumed)
    public event Action<bool> OnPauseStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // Ensure pause menu panel is inactive at start
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(IsPaused);
        }
    }

    /// <summary>
    /// Pauses the game, freezes time, opens pause UI panel, and pauses audio/rhythm.
    /// </summary>
    public void PauseGame()
    {
        if (IsPaused) return;

        IsPaused = true;
        Time.timeScale = 0f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }

        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.PauseRhythm();
        }

        OnPauseStateChanged?.Invoke(true);
        Debug.Log("[SceneController] Game Paused.");
    }

    /// <summary>
    /// Resumes the game, unfreezes time, hides pause UI panel, and resumes audio/rhythm.
    /// </summary>
    public void ResumeGame()
    {
        if (!IsPaused && Time.timeScale == 1f) return;

        IsPaused = false;
        Time.timeScale = 1f;

        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }

        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.ResumeRhythm();
        }

        OnPauseStateChanged?.Invoke(false);
        Debug.Log("[SceneController] Game Resumed.");
    }

    /// <summary>
    /// Toggles between Pause and Resume.
    /// </summary>
    public void TogglePause()
    {
        if (IsPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    /// <summary>
    /// Restarts the currently active scene.
    /// Ensures time scale is reset to 1 before reloading.
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("[SceneController] Restarting current scene...");
        Time.timeScale = 1f;
        IsPaused = false;

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }

    /// <summary>
    /// Loads the Main Menu scene (default "MainMenu").
    /// Ensures time scale is reset to 1 before loading.
    /// </summary>
    public void LoadMainMenu(string mainMenuSceneName = "MainMenu")
    {
        Debug.Log($"[SceneController] Loading Main Menu scene: {mainMenuSceneName}...");
        Time.timeScale = 1f;
        IsPaused = false;

        SceneManager.LoadScene(mainMenuSceneName);
    }

    /// <summary>
    /// Loads a scene by its name in Build Settings.
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SceneController] Scene name is empty!");
            return;
        }
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Quits the application (works in standalone builds and stops playmode in editor).
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[SceneController] Quitting game...");
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}

