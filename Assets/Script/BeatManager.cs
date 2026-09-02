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
    [SerializeField] private float bpm = 120f;
    [SerializeField] private float perfectWindow = 0.08f; // ± Seconds
    [SerializeField] private float goodWindow = 0.15f;    // ± Seconds

    [Header("Audio (Optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playMetronomeClick = false;

    // Events
    public event Action<int> OnBeat;
    public event Action<HitRating> OnHitEvaluated;

    private double songStartTime;
    private float secPerBeat;
    private int currentBeat = -1;
    private bool isPlaying = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        secPerBeat = 60f / bpm;
    }

    private void Start()
    {
        StartRhythm();
    }

    public void StartRhythm()
    {
        secPerBeat = 60f / bpm;
        songStartTime = AudioSettings.dspTime;
        isPlaying = true;
        currentBeat = -1;

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (!isPlaying) return;

        double songTime = AudioSettings.dspTime - songStartTime;
        int beatIndex = (int)(songTime / secPerBeat);

        if (beatIndex > currentBeat)
        {
            currentBeat = beatIndex;
            OnBeat?.Invoke(currentBeat);

            if (playMetronomeClick && audioSource == null)
            {
                // Simple debug log for metronome pulse when audio source isn't set
                Debug.Log($"<color=cyan>[BEAT {currentBeat}]</color>");
            }
        }
    }

    /// <summary>
    /// Evaluates how close the current time is to the nearest beat.
    /// </summary>
    public HitRating EvaluateHitTiming()
    {
        if (!isPlaying) return HitRating.Miss;

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
        if (!isPlaying || secPerBeat <= 0) return 0f;
        double songTime = AudioSettings.dspTime - songStartTime;
        return (float)((songTime / secPerBeat) % 1.0);
    }

    public float Bpm => bpm;
    public float SecPerBeat => secPerBeat;
    public int CurrentBeat => currentBeat;
}
