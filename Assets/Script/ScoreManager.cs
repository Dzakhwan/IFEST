using System;
using UnityEngine;

public enum GameMode
{
    TimeAttack3Min,
    Endless
}

public enum GameRank
{
    S,
    A,
    B,
    C,
    D
}

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Game Mode Configuration")]
    [SerializeField] private GameMode currentGameMode = GameMode.TimeAttack3Min;

    [Header("Base Hit Points Settings")]
    [SerializeField] private int perfectBasePoints = 100;
    [SerializeField] private int goodBasePoints = 50;
    [SerializeField] private int dominoVictimBasePoints = 150;

    [Header("Combo Multiplier Tiers")]
    [SerializeField] private float tier1Multiplier = 1.0f; // Combo 1 - 9
    [SerializeField] private float tier2Multiplier = 1.2f; // Combo 10 - 19
    [SerializeField] private float tier3Multiplier = 1.5f; // Combo 20 - 34
    [SerializeField] private float tier4Multiplier = 2.0f; // Combo 35 - 49
    [SerializeField] private float tier5Multiplier = 2.5f; // Combo >= 50

    [Header("Fever Mode Score Multiplier")]
    [SerializeField] private float feverScoreMultiplier = 2.0f;

    // High Score PlayerPrefs keys
    private const string PREFS_HIGH_SCORE_3MIN = "IFEST_HighScore_3Min";
    private const string PREFS_HIGH_SCORE_ENDLESS = "IFEST_HighScore_Endless";

    // Public Events
    public event Action<int, int, bool> OnScoreChanged; // (currentScore, pointsAdded, isCritical/highCombo)
    public event Action<int> OnMaxComboChanged;       // (maxCombo)
    public event Action OnGameEnded;                  // Triggered on time up / game over

    // Stats
    public GameMode CurrentGameMode => currentGameMode;
    public int CurrentScore { get; private set; } = 0;
    public int HighScore { get; private set; } = 0;
    public int CurrentCombo { get; private set; } = 0;
    public int MaxCombo { get; private set; } = 0;
    public int PerfectCount { get; private set; } = 0;
    public int GoodCount { get; private set; } = 0;
    public int MissCount { get; private set; } = 0;
    public int DominoKillsCount { get; private set; } = 0;
    public bool IsNewHighScore { get; private set; } = false;

    public int TotalHits => PerfectCount + GoodCount;
    public int TotalAttempts => PerfectCount + GoodCount + MissCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Load mode preference if saved
        int savedModeIndex = PlayerPrefs.GetInt("IFEST_SelectedGameMode", (int)GameMode.TimeAttack3Min);
        currentGameMode = (GameMode)savedModeIndex;

        LoadHighScore();
    }

    private void Start()
    {
        ResetRunStats();
    }

    /// <summary>
    /// Sets the active game mode and loads the corresponding High Score.
    /// </summary>
    public void SetGameMode(GameMode mode)
    {
        currentGameMode = mode;
        PlayerPrefs.SetInt("IFEST_SelectedGameMode", (int)mode);
        PlayerPrefs.Save();
        LoadHighScore();
        Debug.Log($"[ScoreManager] Game Mode set to: {mode}");
    }

    /// <summary>
    /// Loads the stored High Score for the current active mode.
    /// </summary>
    public void LoadHighScore()
    {
        string key = (currentGameMode == GameMode.TimeAttack3Min) ? PREFS_HIGH_SCORE_3MIN : PREFS_HIGH_SCORE_ENDLESS;
        HighScore = PlayerPrefs.GetInt(key, 0);
    }

    /// <summary>
    /// Resets all statistics for a new run.
    /// </summary>
    public void ResetRunStats()
    {
        CurrentScore = 0;
        CurrentCombo = 0;
        MaxCombo = 0;
        PerfectCount = 0;
        GoodCount = 0;
        MissCount = 0;
        DominoKillsCount = 0;
        IsNewHighScore = false;
        LoadHighScore();

        OnScoreChanged?.Invoke(CurrentScore, 0, false);
        OnMaxComboChanged?.Invoke(MaxCombo);
    }

    /// <summary>
    /// Calculates current combo multiplier based on combo tier.
    /// </summary>
    public float GetComboMultiplier(int combo)
    {
        if (combo >= 50) return tier5Multiplier;
        if (combo >= 35) return tier4Multiplier;
        if (combo >= 20) return tier3Multiplier;
        if (combo >= 10) return tier2Multiplier;
        return tier1Multiplier;
    }

    /// <summary>
    /// Calculates hit score based on HitRating, current combo, and Fever mode state.
    /// </summary>
    public void AddHitScore(HitRating rating, int combo)
    {
        CurrentCombo = combo;
        if (CurrentCombo > MaxCombo)
        {
            MaxCombo = CurrentCombo;
            OnMaxComboChanged?.Invoke(MaxCombo);
        }

        int basePts = 0;
        if (rating == HitRating.Perfect)
        {
            basePts = perfectBasePoints;
            PerfectCount++;
        }
        else if (rating == HitRating.Good)
        {
            basePts = goodBasePoints;
            GoodCount++;
        }
        else
        {
            RegisterMiss();
            return;
        }

        float comboMult = GetComboMultiplier(CurrentCombo);
        float feverMult = (BeatManager.Instance != null && BeatManager.Instance.IsFeverActive) ? feverScoreMultiplier : 1.0f;

        int finalAddedScore = Mathf.RoundToInt(basePts * comboMult * feverMult);
        CurrentScore += finalAddedScore;

        CheckAndUpdateHighScore();

        bool isHighComboHit = (CurrentCombo >= 20) || (rating == HitRating.Perfect && BeatManager.Instance != null && BeatManager.Instance.IsFeverActive);
        OnScoreChanged?.Invoke(CurrentScore, finalAddedScore, isHighComboHit);

        Debug.Log($"<color=cyan>[SCORE+]</color> +{finalAddedScore} pts (Base:{basePts} x Combo:{comboMult:F1}x x Fever:{feverMult:F1}x) | Total: {CurrentScore}");
    }

    /// <summary>
    /// Adds bonus points for Domino Cascade kills during Fever mode.
    /// </summary>
    public void AddDominoChainScore(int victimCount)
    {
        if (victimCount <= 0) return;

        DominoKillsCount += victimCount;
        float feverMult = (BeatManager.Instance != null && BeatManager.Instance.IsFeverActive) ? feverScoreMultiplier : 1.0f;
        float comboMult = GetComboMultiplier(CurrentCombo);

        int bonusPerVictim = Mathf.RoundToInt(dominoVictimBasePoints * comboMult * feverMult);
        int totalDominoBonus = bonusPerVictim * victimCount;

        CurrentScore += totalDominoBonus;
        CheckAndUpdateHighScore();

        OnScoreChanged?.Invoke(CurrentScore, totalDominoBonus, true);
        Debug.Log($"<color=gold>[DOMINO SCORE BONUS!]</color> +{totalDominoBonus} pts ({victimCount} victims x {bonusPerVictim} pts) | Total: {CurrentScore}");
    }

    /// <summary>
    /// Registers a timing miss or out-of-turn strike.
    /// Resets current combo counter.
    /// </summary>
    public void RegisterMiss()
    {
        MissCount++;
        CurrentCombo = 0;
        Debug.Log($"<color=red>[SCORE MISS]</color> Combo Reset. Total Misses: {MissCount}");
    }

    private void CheckAndUpdateHighScore()
    {
        if (CurrentScore > HighScore)
        {
            HighScore = CurrentScore;
            IsNewHighScore = true;

            string key = (currentGameMode == GameMode.TimeAttack3Min) ? PREFS_HIGH_SCORE_3MIN : PREFS_HIGH_SCORE_ENDLESS;
            PlayerPrefs.SetInt(key, HighScore);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Calculates accuracy percentage (0.0% to 100.0%).
    /// </summary>
    public float GetAccuracyPercentage()
    {
        int total = TotalAttempts;
        if (total <= 0) return 100.0f;

        float scoreWeight = (PerfectCount * 100f) + (GoodCount * 50f);
        float maxPossible = total * 100f;
        return Mathf.Clamp((scoreWeight / maxPossible) * 100f, 0f, 100f);
    }

    /// <summary>
    /// Computes overall performance Rank Grade (S, A, B, C, D).
    /// </summary>
    public GameRank EvaluateRank()
    {
        float accuracy = GetAccuracyPercentage();

        if (accuracy >= 90.0f && MissCount <= 3)
            return GameRank.S;
        if (accuracy >= 80.0f)
            return GameRank.A;
        if (accuracy >= 70.0f)
            return GameRank.B;
        if (accuracy >= 55.0f)
            return GameRank.C;

        return GameRank.D;
    }

    /// <summary>
    /// Concludes the current game run and triggers end game handlers.
    /// </summary>
    public void EndGameRun()
    {
        CheckAndUpdateHighScore();
        OnGameEnded?.Invoke();
        Debug.Log($"<color=gold>[GAME ENDED]</color> Final Score: {CurrentScore}, Max Combo: {MaxCombo}, Accuracy: {GetAccuracyPercentage():F1}%, Rank: {EvaluateRank()}");
    }
}
