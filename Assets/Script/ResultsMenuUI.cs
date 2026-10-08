using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ResultsMenuUI : MonoBehaviour
{
    [Header("Canvas Panel Reference (Optional)")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private TextMeshProUGUI titleTextTMP;
    [SerializeField] private TextMeshProUGUI finalScoreTextTMP;
    [SerializeField] private TextMeshProUGUI highScoreTextTMP;
    [SerializeField] private TextMeshProUGUI rankTextTMP;
    [SerializeField] private TextMeshProUGUI maxComboTextTMP;
    [SerializeField] private TextMeshProUGUI accuracyTextTMP;
    [SerializeField] private TextMeshProUGUI breakdownTextTMP;

    [Header("OnGUI Fallback Settings")]
    [SerializeField] private bool useOnGUIFallback = true;

    [Header("Audio Feedback")]
    [SerializeField] private AudioClip resultSound;
    [SerializeField] private AudioSource audioSource;

    private bool isDisplayingResults = false;

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
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnGameEnded += ShowResults;
        }
    }

    private void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnGameEnded -= ShowResults;
        }
    }

    private void Start()
    {
        if (resultsPanel != null)
        {
            resultsPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Displays the results panel and updates text components with final stats.
    /// </summary>
    public void ShowResults()
    {
        isDisplayingResults = true;

        if (resultSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(resultSound);
        }

        if (resultsPanel != null)
        {
            resultsPanel.SetActive(true);
        }

        UpdateCanvasUI();
    }

    private void UpdateCanvasUI()
    {
        if (ScoreManager.Instance == null) return;

        ScoreManager sm = ScoreManager.Instance;
        GameRank rank = sm.EvaluateRank();

        if (titleTextTMP != null)
        {
            titleTextTMP.text = sm.CurrentGameMode == GameMode.TimeAttack3Min ? "3-MINUTE TIME ATTACK" : "ENDLESS MODE RESULTS";
        }

        if (finalScoreTextTMP != null)
        {
            finalScoreTextTMP.text = $"FINAL SCORE: {sm.CurrentScore:N0}";
        }

        if (highScoreTextTMP != null)
        {
            string newBadge = sm.IsNewHighScore ? " [NEW HIGH SCORE!]" : "";
            highScoreTextTMP.text = $"HIGH SCORE: {sm.HighScore:N0}{newBadge}";
        }

        if (rankTextTMP != null)
        {
            rankTextTMP.text = $"RANK: {rank}";
            rankTextTMP.color = GetRankColor(rank);
        }

        if (maxComboTextTMP != null)
        {
            maxComboTextTMP.text = $"MAX COMBO: {sm.MaxCombo}X";
        }

        if (accuracyTextTMP != null)
        {
            accuracyTextTMP.text = $"ACCURACY: {sm.GetAccuracyPercentage():F1}%";
        }

        if (breakdownTextTMP != null)
        {
            breakdownTextTMP.text = $"PERFECT: {sm.PerfectCount}  |  GOOD: {sm.GoodCount}  |  MISS: {sm.MissCount}  |  DOMINO: {sm.DominoKillsCount}";
        }
    }

    private Color GetRankColor(GameRank rank)
    {
        switch (rank)
        {
            case GameRank.S: return new Color(1.0f, 0.84f, 0.0f); // Gold
            case GameRank.A: return new Color(0.2f, 0.95f, 0.3f); // Emerald Green
            case GameRank.B: return new Color(0.15f, 0.75f, 1.0f); // Bright Blue
            case GameRank.C: return new Color(1.0f, 0.75f, 0.15f); // Orange
            default: return new Color(0.95f, 0.25f, 0.25f);       // Red
        }
    }

    // UI Canvas Button Callbacks
    public void OnRestartButtonClicked()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.RestartGame();
        }
    }

    public void OnMainMenuButtonClicked()
    {
        if (SceneController.Instance != null)
        {
            SceneController.Instance.LoadMainMenu();
        }
    }

    private void OnGUI()
    {
        if (!useOnGUIFallback || !isDisplayingResults) return;
        if (ScoreManager.Instance == null) return;

        ScoreManager sm = ScoreManager.Instance;
        GameRank rank = sm.EvaluateRank();

        float boxWidth = 380f;
        float boxHeight = 360f;
        float startX = (Screen.width - boxWidth) / 2f;
        float startY = (Screen.height - boxHeight) / 2f;

        // Background Box
        GUI.Box(new Rect(startX, startY, boxWidth, boxHeight), "");

        // Header Title
        GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            fontStyle = FontStyle.Bold
        };
        headerStyle.normal.textColor = Color.yellow;
        string modeTitle = sm.CurrentGameMode == GameMode.TimeAttack3Min ? "TIME'S UP! (3-MIN TIME ATTACK)" : "GAME OVER (ENDLESS MODE)";
        GUI.Label(new Rect(startX, startY + 15, boxWidth, 30), modeTitle, headerStyle);

        // Rank Display
        GUIStyle rankStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 44,
            fontStyle = FontStyle.Bold
        };
        rankStyle.normal.textColor = GetRankColor(rank);
        GUI.Label(new Rect(startX, startY + 45, boxWidth, 50), $"RANK {rank}", rankStyle);

        // Stats Labels
        GUIStyle statsStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
        statsStyle.normal.textColor = Color.white;

        GUI.Label(new Rect(startX, startY + 105, boxWidth, 24), $"FINAL SCORE: {sm.CurrentScore:N0}", statsStyle);

        string highScoreStr = sm.IsNewHighScore ? $"HIGH SCORE: {sm.HighScore:N0} ★ NEW RECORD!" : $"HIGH SCORE: {sm.HighScore:N0}";
        GUIStyle hsStyle = new GUIStyle(statsStyle);
        if (sm.IsNewHighScore) hsStyle.normal.textColor = new Color(1.0f, 0.85f, 0.2f);
        GUI.Label(new Rect(startX, startY + 130, boxWidth, 24), highScoreStr, hsStyle);

        GUI.Label(new Rect(startX, startY + 160, boxWidth, 24), $"ACCURACY: {sm.GetAccuracyPercentage():F1}%  |  MAX COMBO: {sm.MaxCombo}X", statsStyle);

        // Hit breakdown
        GUIStyle detailStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13
        };
        detailStyle.normal.textColor = new Color(0.85f, 0.85f, 0.9f);
        GUI.Label(new Rect(startX, startY + 195, boxWidth, 24), $"Perfect: {sm.PerfectCount}  |  Good: {sm.GoodCount}  |  Miss: {sm.MissCount}", detailStyle);
        GUI.Label(new Rect(startX, startY + 218, boxWidth, 24), $"Domino Chain Kills: {sm.DominoKillsCount}", detailStyle);

        // Buttons
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };

        if (GUI.Button(new Rect(startX + 40, startY + 255, boxWidth - 80, 40), "PLAY AGAIN ↺", buttonStyle))
        {
            OnRestartButtonClicked();
        }

        if (GUI.Button(new Rect(startX + 40, startY + 305, boxWidth - 80, 40), "MAIN MENU 🏠", buttonStyle))
        {
            OnMainMenuButtonClicked();
        }
    }
}
