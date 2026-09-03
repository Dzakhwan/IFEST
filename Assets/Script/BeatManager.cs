using System;
using UnityEngine;

public enum HitRating
{
    Miss,
    Good,
    Perfect
}

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("Rhythm Settings")]
    [SerializeField] private float baseBpm = 120f;
    [SerializeField] private float bpmPerCombo = 3f;      // Add 3 BPM per combo streak
    [SerializeField] private float maxBpm = 220f;          // Maximum tempo cap
    [SerializeField] private float perfectWindow = 0.08f;  // ± Seconds
    [SerializeField] private float goodWindow = 0.15f;     // ± Seconds

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playMetronomeClick = false;
    [SerializeField] private bool syncAudioPitchWithBpm = true;

    // Events
    public event Action<int> OnBeat;
    public event Action<HitRating> OnHitEvaluated;
    public event Action<float> OnBpmChanged;

    private float currentBpm;
    private double songStartTime;
    private double pauseStartTime;
    private float secPerBeat;
    private int currentBeat = -1;
    private bool isPlaying = false;
    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentBpm = baseBpm;
        secPerBeat = 60f / currentBpm;
    }

    private void Start()
    {
        StartRhythm();
    }

    public void StartRhythm()
    {
        currentBpm = baseBpm;
        secPerBeat = 60f / currentBpm;
        songStartTime = AudioSettings.dspTime;
        isPlaying = true;
        isPaused = false;
        currentBeat = -1;

        if (audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Pauses rhythm tracking and audio playback.
    /// </summary>
    public void PauseRhythm()
    {
        if (!isPlaying || isPaused) return;

        isPaused = true;
        pauseStartTime = AudioSettings.dspTime;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    /// <summary>
    /// Resumes rhythm tracking and audio playback, adjusting timing offsets.
    /// </summary>
    public void ResumeRhythm()
    {
        if (!isPlaying || !isPaused) return;

        double pauseDuration = AudioSettings.dspTime - pauseStartTime;
        songStartTime += pauseDuration;
        isPaused = false;

        if (audioSource != null)
        {
            audioSource.UnPause();
        }
    }

    /// <summary>
    /// Dynamically scales BPM based on the player's active combo count.
    /// </summary>
    public void SetCombo(int comboCount)
    {
        float targetBpm = Mathf.Min(baseBpm + (comboCount * bpmPerCombo), maxBpm);
        if (Mathf.Approximately(targetBpm, currentBpm)) return;

        currentBpm = targetBpm;
        secPerBeat = 60f / currentBpm;

        // Sync Audio pitch if audio source exists
        if (audioSource != null && syncAudioPitchWithBpm && baseBpm > 0)
        {
            audioSource.pitch = currentBpm / baseBpm;
        }

        OnBpmChanged?.Invoke(currentBpm);
        Debug.Log($"<color=orange>[TEMPO SPEED UP]</color> Combo x{comboCount} ➔ BPM: {currentBpm:F1}");
    }

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        double songTime = AudioSettings.dspTime - songStartTime;
        int beatIndex = (int)(songTime / secPerBeat);

        if (beatIndex > currentBeat)
        {
            currentBeat = beatIndex;
            OnBeat?.Invoke(currentBeat);

            if (playMetronomeClick && audioSource == null)
            {
                Debug.Log($"<color=cyan>[BEAT {currentBeat}] BPM: {currentBpm:F0}</color>");
            }
        }
    }

    /// <summary>
    /// Evaluates how close the current time is to the nearest beat.
    /// </summary>
    public HitRating EvaluateHitTiming()
    {
        if (!isPlaying || isPaused) return HitRating.Miss;

        double songTime = AudioSettings.dspTime - songStartTime;
        double nearestBeatTime = Math.Round(songTime / secPerBeat) * secPerBeat;
        float offset = (float)Math.Abs(songTime - nearestBeatTime);

        HitRating rating;
        if (offset <= perfectWindow)
        {
            rating = HitRating.Perfect;
        }
        else if (offset <= goodWindow)
        {
            rating = HitRating.Good;
        }
        else
        {
            rating = HitRating.Miss;
        }

        OnHitEvaluated?.Invoke(rating);
        return rating;
    }

    /// <summary>
    /// Returns normalized beat progress (0.0 to 1.0) between beats for UI animations.
    /// </summary>
    public float GetBeatProgress()
    {
        if (!isPlaying || isPaused || secPerBeat <= 0) return 0f;
        double songTime = AudioSettings.dspTime - songStartTime;
        return (float)((songTime / secPerBeat) % 1.0);
    }

    public float BaseBpm => baseBpm;
    public float Bpm => currentBpm;
    public float SecPerBeat => secPerBeat;
    public int CurrentBeat => currentBeat;
}
