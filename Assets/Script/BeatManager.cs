using System;
using UnityEngine;

public enum HitRating
{
    Miss,
    Good,
    Perfect
}

public enum GameRhythmState
{
    Normal,
    ToFever,
    Fever
}

public class BeatManager : MonoBehaviour
{
    public static BeatManager Instance { get; private set; }

    [Header("Rhythm Settings")]
    [SerializeField] private float baseBpm = 120f;
    [SerializeField] private float bpmPerCombo = 1.5f;
    [SerializeField] private float maxBpm = 220f;
    [SerializeField] private float perfectWindow = 0.12f;  // ± Seconds
    [SerializeField] private float goodWindow = 0.22f;     // ± Seconds

    [Header("Audio Calibration & Offset (Per-Song)")]
    [Tooltip("Offset for Normal Track in seconds (default 0).")]
    [SerializeField] private float normalSongOffset = 0.0f;
    [Tooltip("Offset for To Fever Track in seconds (tune specifically for To Fever).")]
    [SerializeField] private float toFeverSongOffset = 0.0f;
    [Tooltip("Offset for Fever Track in seconds (default 0).")]
    [SerializeField] private float feverSongOffset = 0.0f;
    [SerializeField] private bool playMetronomeClick = false;

    [Header("Audio Tracks (3-Phase Rhythm)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip normalTrack;
    [SerializeField] private AudioClip toFeverTrack;
    [SerializeField] private AudioClip feverTrack;
    [SerializeField] private bool syncAudioPitchWithBpm = true;

    [Header("Fever Settings")]
    [SerializeField] private int comboToTriggerToFever = 16;
    [SerializeField] private int hitsToCompleteToFever = 4;
    [SerializeField] private float toFeverDuration = 15f;
    [SerializeField] private float feverDuration = 31f;

    // Events
    public event Action<int> OnBeat;
    public event Action<HitRating> OnHitEvaluated;
    public event Action<float> OnBpmChanged;
    public event Action<GameRhythmState> OnStateChanged;
    public event Action<int, int> OnNormalFeverProgressChanged; // (currentProgress, maxProgress)
    public event Action<int, int> OnToFeverProgressChanged; // (currentHits, targetHits)
    public event Action<float> OnStateTimerUpdated;        // remaining seconds

    // State Machine
    public GameRhythmState CurrentState { get; private set; } = GameRhythmState.Normal;
    public bool IsFeverActive => CurrentState == GameRhythmState.Fever;
    public int ComboToTriggerToFever => comboToTriggerToFever;
    public int HitsToCompleteToFever => hitsToCompleteToFever;
    public int NormalFeverProgress { get; private set; } = 0;
    public int CurrentToFeverHits { get; private set; } = 0;
    public float StateTimeRemaining { get; private set; } = 0f;
    public float FeverDuration => (feverTrack != null && feverTrack.length > 0) ? feverTrack.length : feverDuration;

    /// <summary>
    /// Returns the active song offset based on current music state.
    /// </summary>
    public float CurrentSongOffset
    {
        get
        {
            switch (CurrentState)
            {
                case GameRhythmState.ToFever: return toFeverSongOffset;
                case GameRhythmState.Fever: return feverSongOffset;
                default: return normalSongOffset;
            }
        }
    }

    public float SongOffset => CurrentSongOffset;

    private float currentBpm;
    private double songStartTime;
    private double pauseStartTime;
    private float secPerBeat;
    private int currentBeat = -1;
    private bool isPlaying = false;
    private bool isPaused = false;

    // Procedural metronome click sound
    private AudioClip clickClip;
    private AudioSource clickSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (normalTrack == null && audioSource != null && audioSource.clip != null)
        {
            normalTrack = audioSource.clip;
        }

        currentBpm = baseBpm;
        secPerBeat = 60f / currentBpm;

        CreateProceduralClick();
    }

    private void CreateProceduralClick()
    {
        int sampleRate = 44100;
        int sampleLength = (int)(sampleRate * 0.04f); // 40ms click
        float[] samples = new float[sampleLength];
        float frequency = 1200f; // 1.2 kHz crisp woodblock/tick frequency

        for (int i = 0; i < sampleLength; i++)
        {
            float t = (float)i / sampleRate;
            float decay = Mathf.Exp(-t * 120f); // Exponential decay
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * decay * 0.85f;
        }

        clickClip = AudioClip.Create("ProceduralMetronomeClick", sampleLength, 1, sampleRate, false);
        clickClip.SetData(samples, 0);

        clickSource = gameObject.AddComponent<AudioSource>();
        clickSource.playOnAwake = false;
        clickSource.volume = 0.85f;
    }

    private void Start()
    {
        StartRhythm();
    }

    public void StartRhythm()
    {
        currentBpm = baseBpm;
        secPerBeat = 60f / currentBpm;
        isPlaying = true;
        isPaused = false;

        StartNormalState();
    }

    public void StartNormalState()
    {
        CurrentState = GameRhythmState.Normal;
        CurrentToFeverHits = 0;
        NormalFeverProgress = 0;
        OnNormalFeverProgressChanged?.Invoke(0, comboToTriggerToFever);
        StateTimeRemaining = 0f;

        if (audioSource != null && normalTrack != null)
        {
            audioSource.clip = normalTrack;
            audioSource.loop = true;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }

        songStartTime = AudioSettings.dspTime;
        currentBeat = -1;

        OnStateChanged?.Invoke(CurrentState);
        Debug.Log("<color=cyan>[RHYTHM STATE]</color> Switched to NORMAL TRACK. Fever Bar reset to 0.");
    }

    public void StartToFeverState()
    {
        if (CurrentState != GameRhythmState.Normal) return;

        CurrentState = GameRhythmState.ToFever;
        CurrentToFeverHits = 0;
        NormalFeverProgress = 0;
        OnNormalFeverProgressChanged?.Invoke(0, comboToTriggerToFever);
        StateTimeRemaining = (toFeverTrack != null && toFeverTrack.length > 0) ? toFeverTrack.length : toFeverDuration;

        if (audioSource != null && toFeverTrack != null)
        {
            audioSource.clip = toFeverTrack;
            audioSource.loop = false;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }

        songStartTime = AudioSettings.dspTime;
        currentBeat = -1;

        OnStateChanged?.Invoke(CurrentState);
        OnToFeverProgressChanged?.Invoke(CurrentToFeverHits, hitsToCompleteToFever);
        Debug.Log($"<color=yellow>[TO FEVER STARTED!]</color> Challenge: Hit {hitsToCompleteToFever} times within {StateTimeRemaining:F1}s! (Offset: {toFeverSongOffset:+0.00;-0.00;0.00}s)");
    }

    /// <summary>
    /// Adds 1 progress to the Normal mode Fever gauge.
    /// Only accumulates during Normal state. Automatically triggers ToFever upon reaching threshold.
    /// </summary>
    public void AddNormalFeverProgress()
    {
        if (CurrentState != GameRhythmState.Normal) return;

        NormalFeverProgress++;
        OnNormalFeverProgressChanged?.Invoke(NormalFeverProgress, comboToTriggerToFever);
        Debug.Log($"<color=cyan>[FEVER GAUGE]</color> {NormalFeverProgress}/{comboToTriggerToFever}");

        if (NormalFeverProgress >= comboToTriggerToFever)
        {
            ResetNormalFeverProgress();
            StartToFeverState();
        }
    }

    /// <summary>
    /// Resets the Normal mode Fever gauge back to 0.
    /// Triggered when the player misses timing, hits out of turn, or whiffs during Normal mode.
    /// </summary>
    public void ResetNormalFeverProgress()
    {
        NormalFeverProgress = 0;
        OnNormalFeverProgressChanged?.Invoke(0, comboToTriggerToFever);
    }

    public void RegisterToFeverHit()
    {
        if (CurrentState != GameRhythmState.ToFever) return;

        CurrentToFeverHits++;
        OnToFeverProgressChanged?.Invoke(CurrentToFeverHits, hitsToCompleteToFever);
        Debug.Log($"<color=yellow>[TO FEVER PROGRESS]</color> {CurrentToFeverHits}/{hitsToCompleteToFever} Hits!");

        if (CurrentToFeverHits >= hitsToCompleteToFever)
        {
            Debug.Log("<color=green>[TO FEVER TARGET MET!]</color> Challenge passed! Waiting for buildup to finish before dropping into Fever!");
        }
    }

    /// <summary>
    /// Resets ToFever hits if player misses before reaching the 4-hit threshold.
    /// Once the 4-hit threshold is reached, success is locked in until the song finishes.
    /// </summary>
    public void ResetToFeverHitsOnMiss()
    {
        if (CurrentState != GameRhythmState.ToFever) return;

        if (CurrentToFeverHits < hitsToCompleteToFever)
        {
            CurrentToFeverHits = 0;
            OnToFeverProgressChanged?.Invoke(0, hitsToCompleteToFever);
            Debug.Log("<color=orange>[TO FEVER RESET]</color> Missed before reaching target hits! To Fever counter reset to 0.");
        }
    }

    public void StartFeverState()
    {
        CurrentState = GameRhythmState.Fever;
        CurrentToFeverHits = 0;
        NormalFeverProgress = 0;
        StateTimeRemaining = (feverTrack != null && feverTrack.length > 0) ? feverTrack.length : feverDuration;

        if (audioSource != null && feverTrack != null)
        {
            audioSource.clip = feverTrack;
            audioSource.loop = false;
            audioSource.pitch = 1.0f;
            audioSource.Play();
        }

        songStartTime = AudioSettings.dspTime;
        currentBeat = -1;

        OnStateChanged?.Invoke(CurrentState);
        Debug.Log($"<color=red>[🔥 FEVER MODE ACTIVE! 🔥]</color> Domino Chain Reaction ENABLED for {StateTimeRemaining:F1}s!");
    }

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

    public void SetCombo(int comboCount)
    {
        // Dynamic tempo progression (currently constant)
    }

    private void Update()
    {
        if (!isPlaying || isPaused) return;

        // State timer updates
        if (CurrentState == GameRhythmState.ToFever)
        {
            StateTimeRemaining -= Time.deltaTime;
            OnStateTimerUpdated?.Invoke(StateTimeRemaining);

            if (StateTimeRemaining <= 0f)
            {
                // When ToFever song finishes: check if challenge passed
                if (CurrentToFeverHits >= hitsToCompleteToFever)
                {
                    Debug.Log("<color=green>[BEAT DROP!]</color> To Fever buildup complete! Dropping into FEVER MODE!");
                    StartFeverState();
                }
                else
                {
                    Debug.Log("<color=orange>[TO FEVER TIMEOUT]</color> Failed to reach 4 hits in time. Returning to Normal.");
                    StartNormalState();
                }
            }
        }
        else if (CurrentState == GameRhythmState.Fever)
        {
            StateTimeRemaining -= Time.deltaTime;
            OnStateTimerUpdated?.Invoke(StateTimeRemaining);

            if (StateTimeRemaining <= 0f)
            {
                Debug.Log("<color=cyan>[FEVER ENDED]</color> Fever duration complete. Returning to Normal.");
                StartNormalState();
            }
        }

        // Beat tracking with CurrentSongOffset applied
        double songTime = (AudioSettings.dspTime - songStartTime) - CurrentSongOffset;
        if (songTime >= 0)
        {
            int beatIndex = (int)(songTime / secPerBeat);

            if (beatIndex > currentBeat)
            {
                currentBeat = beatIndex;
                OnBeat?.Invoke(currentBeat);

                if (playMetronomeClick)
                {
                    PlayProceduralClick();
                }
            }
        }
    }

    private void PlayProceduralClick()
    {
        if (clickSource != null && clickClip != null)
        {
            clickSource.PlayOneShot(clickClip);
        }
    }

    public HitRating EvaluateHitTiming()
    {
        if (!isPlaying || isPaused) return HitRating.Miss;

        double songTime = (AudioSettings.dspTime - songStartTime) - CurrentSongOffset;
        if (songTime < 0) songTime = 0;

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

    public float GetBeatProgress()
    {
        if (!isPlaying || isPaused || secPerBeat <= 0) return 0f;
        double songTime = (AudioSettings.dspTime - songStartTime) - CurrentSongOffset;
        if (songTime < 0) return 0f;
        return (float)((songTime / secPerBeat) % 1.0);
    }

    /// <summary>
    /// Returns signed offset to nearest beat in seconds.
    /// Negative = approaching beat (early). Positive = past beat (late).
    /// </summary>
    public float GetSignedOffsetToNearestBeat()
    {
        if (!isPlaying || isPaused || secPerBeat <= 0) return 0f;
        double songTime = (AudioSettings.dspTime - songStartTime) - CurrentSongOffset;
        if (songTime < 0) return 0f;
        double nearestBeat = Math.Round(songTime / secPerBeat) * secPerBeat;
        return (float)(songTime - nearestBeat);
    }

    public float BaseBpm => baseBpm;
    public float Bpm => currentBpm;
    public float SecPerBeat => secPerBeat;
    public int CurrentBeat => currentBeat;
    public float PerfectWindow => perfectWindow;
    public float GoodWindow => goodWindow;
}
