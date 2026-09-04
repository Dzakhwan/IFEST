using UnityEngine;

public class PauseMenuUI : MonoBehaviour
{
    [Header("UI Canvas Panel Reference (Optional)")]
    [SerializeField] private GameObject pauseMenuPanel;

    [Header("UI Click Audio Feedback")]
    [SerializeField] private AudioClip clickSound;
    [SerializeField] private AudioSource audioSource;

    [Header("OnGUI Fallback UI Settings")]
    [SerializeField] private bool useOnGUIFallback = true;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnEnable()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnPauseStateChanged += HandlePauseStateChanged;
        }
    }

    private void OnDisable()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.OnPauseStateChanged -= HandlePauseStateChanged;
        }
    }

    private void Start()
    {
        // Ensure panel state reflects current pause state at startup
        if (SceneController.Instance != null)
        {
            HandlePauseStateChanged(SceneController.Instance.IsPaused);
        }
    }

    private void HandlePauseStateChanged(bool isPaused)
    {
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(isPaused);
        }
    }

    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    // UI Canvas Button Callbacks
    public void OnPauseButtonClicked()
    {
        PlayClickSound();
        if (SceneController.Instance != null)
        {
            SceneController.Instance.PauseGame();
        }
    }

    public void OnResumeButtonClicked()
    {
        PlayClickSound();
        if (SceneController.Instance != null)
        {
            SceneController.Instance.ResumeGame();
        }
    }

    public void OnRestartButtonClicked()
    {
        PlayClickSound();
        if (SceneController.Instance != null)
        {
            SceneController.Instance.RestartGame();
        }
    }

    public void OnMainMenuButtonClicked()
    {
        PlayClickSound();
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
    }

    public void OnQuitButtonClicked()
    {
        PlayClickSound();
        if (SceneController.Instance != null)
        {
            SceneController.Instance.QuitGame();
        }
    }

    // OnGUI Debug/Fallback UI
    private void OnGUI()
    {
        if (!useOnGUIFallback) return;

        bool isPaused = SceneController.Instance != null && SceneController.Instance.IsPaused;

        // Top-left Pause Button when not paused
        if (!isPaused)
        {
            GUIStyle pauseBtnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            if (GUI.Button(new Rect(20, 20, 100, 40), "PAUSE ❚❚", pauseBtnStyle))
            {
                OnPauseButtonClicked();
            }
        }
        else
        {
            // Centered Pause Overlay Menu
            float menuWidth = 280f;
            float menuHeight = 260f;
            float startX = (Screen.width - menuWidth) / 2f;
            float startY = (Screen.height - menuHeight) / 2f;

            GUI.Box(new Rect(startX, startY, menuWidth, menuHeight), "PAUSE MENU");

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fontStyle = FontStyle.Bold
            };
            GUI.Label(new Rect(startX, startY + 15, menuWidth, 30), "GAME PAUSED", titleStyle);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold
            };

            if (GUI.Button(new Rect(startX + 30, startY + 60, menuWidth - 60, 38), "RESUME ▶", buttonStyle))
            {
                OnResumeButtonClicked();
            }

            if (GUI.Button(new Rect(startX + 30, startY + 108, menuWidth - 60, 38), "RESTART ↺", buttonStyle))
            {
                OnRestartButtonClicked();
            }

            if (GUI.Button(new Rect(startX + 30, startY + 156, menuWidth - 60, 38), "MAIN MENU 🏠", buttonStyle))
            {
                OnMainMenuButtonClicked();
            }

            if (GUI.Button(new Rect(startX + 30, startY + 204, menuWidth - 60, 38), "QUIT ✖", buttonStyle))
            {
                OnQuitButtonClicked();
            }
        }
    }
}
