using System;
using UnityEngine;

public class TimeAttackManager : MonoBehaviour
{
    public static TimeAttackManager Instance { get; private set; }

    [Header("Timer Settings")]
    [Tooltip("Total duration for 3-Minute Time Attack mode in seconds (Default: 180 seconds).")]
    [SerializeField] private float timeAttackDurationSeconds = 180f; // 3 minutes

    [Header("Auto Start")]
    [SerializeField] private bool autoStartOnRhythmBegin = true;

    // Events
    public event Action<float, string> OnTimerUpdated; // (remainingSeconds, formattedMMSS)
    public event Action OnTimeUp;                      // Triggered when timer reaches 0

    // Properties
    public float TimeRemaining { get; private set; }
    public float TotalDuration => timeAttackDurationSeconds;
    public bool IsTimerRunning { get; private set; } = false;
    public bool IsTimeUp { get; private set; } = false;

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
        TimeRemaining = timeAttackDurationSeconds;

        if (autoStartOnRhythmBegin)
        {
            StartTimer();
        }
    }

    /// <summary>
    /// Starts or restarts the 3-Minute countdown timer.
    /// </summary>
    public void StartTimer()
    {
        TimeRemaining = timeAttackDurationSeconds;
        IsTimerRunning = true;
        IsTimeUp = false;

        string formatted = FormatTime(TimeRemaining);
        OnTimerUpdated?.Invoke(TimeRemaining, formatted);
        Debug.Log($"<color=yellow>[TIME ATTACK]</color> 3-Minute Timer Started! ({formatted})");
    }

    /// <summary>
    /// Pauses the countdown timer.
    /// </summary>
    public void PauseTimer()
    {
        IsTimerRunning = false;
    }

    /// <summary>
    /// Resumes the countdown timer.
    /// </summary>
    public void ResumeTimer()
    {
        if (!IsTimeUp)
        {
            IsTimerRunning = true;
        }
    }

    /// <summary>
    /// Resets timer to initial duration.
    /// </summary>
    public void ResetTimer()
    {
        TimeRemaining = timeAttackDurationSeconds;
        IsTimerRunning = false;
        IsTimeUp = false;

        string formatted = FormatTime(TimeRemaining);
        OnTimerUpdated?.Invoke(TimeRemaining, formatted);
    }

    private void Update()
    {
        // Only run timer if current mode is TimeAttack3Min and timer is running
        if (ScoreManager.Instance != null && ScoreManager.Instance.CurrentGameMode != GameMode.TimeAttack3Min)
        {
            return;
        }

        if (!IsTimerRunning || IsTimeUp) return;

        // Check if game is paused
        if (SceneController.Instance != null && SceneController.Instance.IsPaused)
        {
            return;
        }

        TimeRemaining -= Time.deltaTime;
        if (TimeRemaining < 0f)
        {
            TimeRemaining = 0f;
        }

        string formatted = FormatTime(TimeRemaining);
        OnTimerUpdated?.Invoke(TimeRemaining, formatted);

        if (TimeRemaining <= 0f && !IsTimeUp)
        {
            HandleTimeUp();
        }
    }

    private void HandleTimeUp()
    {
        IsTimeUp = true;
        IsTimerRunning = false;

        Debug.Log("<color=red>[TIME ATTACK - TIME'S UP!]</color> 3 Minutes completed! Concluding run...");

        // Pause beat manager & freeze inputs
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.PauseRhythm();
        }

        OnTimeUp?.Invoke();

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.EndGameRun();
        }
    }

    /// <summary>
    /// Formats seconds float into MM:SS string.
    /// </summary>
    public static string FormatTime(float totalSeconds)
    {
        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
