using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RhythmUI : MonoBehaviour
{
    [Header("Custom Rating Sprites (2 Sprites Each)")]
    [SerializeField] private Sprite[] perfectSprites = new Sprite[2];
    [SerializeField] private Sprite[] goodSprites = new Sprite[2];
    [SerializeField] private Sprite[] missSprites = new Sprite[2];

    [Header("UI Canvas Display Reference (Optional)")]
    [SerializeField] private Image ratingImageDisplay;

    [Header("Visual Metronome Settings")]
    [SerializeField] private bool showVisualMetronome = true;
    [SerializeField] private bool useOnGUIDebugUI = false;

    private Sprite currentRatingSprite;
    private int currentCombo = 0;
    private float displayTimer = 0f;

    // Metronome GUI assets
    private static Texture2D whiteTex;
    private float beatFlashIntensity = 0f;

    private void Awake()
    {
        if (whiteTex == null)
        {
            whiteTex = new Texture2D(1, 1);
            whiteTex.SetPixel(0, 0, Color.white);
            whiteTex.Apply();
        }
    }

    private void Start()
    {
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null)
        {
            combat.OnAttackExecuted += OnAttackResult;
        }

        if (ratingImageDisplay != null)
        {
            ratingImageDisplay.gameObject.SetActive(false);
        }

        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat += HandleBeatPulse;
        }
    }

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= HandleBeatPulse;
        }
    }

    private void HandleBeatPulse(int beat)
    {
        beatFlashIntensity = 1.0f;
    }

    private void Update()
    {
        if (beatFlashIntensity > 0f)
        {
            beatFlashIntensity = Mathf.Max(0f, beatFlashIntensity - Time.deltaTime * 6f);
        }
    }

    private void OnAttackResult(HitRating rating, int combo, string customMessage)
    {
        currentCombo = combo;

        // Ignore NO TARGET messages (do not display any rating popup)
        if (!string.IsNullOrEmpty(customMessage) && customMessage.Contains("NO TARGET"))
        {
            ClearDisplay();
            return;
        }

        // Select sprite based on rating
        Sprite selectedSprite = null;

        if (customMessage.Contains("OUT OF TURN") || customMessage.Contains("MISS") || rating == HitRating.Miss)
        {
            selectedSprite = GetRandomSprite(missSprites);
        }
        else if (rating == HitRating.Perfect)
        {
            selectedSprite = GetRandomSprite(perfectSprites);
        }
        else if (rating == HitRating.Good)
        {
            selectedSprite = GetRandomSprite(goodSprites);
        }

        currentRatingSprite = selectedSprite;
        displayTimer = 1.2f;

        if (ratingImageDisplay != null)
        {
            if (selectedSprite != null)
            {
                ratingImageDisplay.sprite = selectedSprite;
                ratingImageDisplay.gameObject.SetActive(true);
            }
            else
            {
                ratingImageDisplay.gameObject.SetActive(false);
            }
        }

        StopAllCoroutines();
        StartCoroutine(ClearDisplayRoutine());
    }

    private Sprite GetRandomSprite(Sprite[] spriteArray)
    {
        if (spriteArray == null || spriteArray.Length == 0) return null;

        List<Sprite> validSprites = new List<Sprite>();
        foreach (var s in spriteArray)
        {
            if (s != null) validSprites.Add(s);
        }

        if (validSprites.Count == 0) return null;
        return validSprites[Random.Range(0, validSprites.Count)];
    }

    private void ClearDisplay()
    {
        currentRatingSprite = null;
        displayTimer = 0f;
        if (ratingImageDisplay != null)
        {
            ratingImageDisplay.gameObject.SetActive(false);
        }
    }

    private IEnumerator ClearDisplayRoutine()
    {
        while (displayTimer > 0)
        {
            displayTimer -= Time.unscaledDeltaTime;
            yield return null;
        }
        ClearDisplay();
    }

    private void OnGUI()
    {
        // 1. Render Visual Metronome Bar
        if (showVisualMetronome && BeatManager.Instance != null)
        {
            DrawVisualMetronome();
        }

        // 2. Fallback OnGUI rating rendering if enabled
        if (useOnGUIDebugUI && ratingImageDisplay == null && currentRatingSprite != null && currentRatingSprite.texture != null)
        {
            Texture2D tex = currentRatingSprite.texture;
            float spriteWidth = tex.width;
            float spriteHeight = tex.height;
            Rect drawRect = new Rect((Screen.width - spriteWidth) / 2f, (Screen.height - spriteHeight) / 2f - 50f, spriteWidth, spriteHeight);

            GUI.DrawTexture(drawRect, tex, ScaleMode.ScaleToFit);

            if (currentCombo > 1)
            {
                GUIStyle comboStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 26,
                    fontStyle = FontStyle.Bold
                };
                comboStyle.normal.textColor = Color.yellow;

                GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f + spriteHeight / 2f, 400, 40), $"COMBO x{currentCombo}", comboStyle);
            }
        }
    }

    private void DrawVisualMetronome()
    {
        float barWidth = 380f;
        float barHeight = 22f;
        float barX = (Screen.width - barWidth) / 2f;
        float barY = Screen.height - 75f;
        float centerX = barX + barWidth / 2f;

        Color prevColor = GUI.color;
        GameRhythmState state = BeatManager.Instance.CurrentState;

        // 1. FEVER GAUGE BAR (Positioned just above Metronome)
        float gaugeY = barY - 16f;
        float gaugeHeight = 10f;

        // Gauge background
        GUI.color = new Color(0.05f, 0.05f, 0.08f, 0.9f);
        GUI.DrawTexture(new Rect(barX - 2, gaugeY - 2, barWidth + 4, gaugeHeight + 4), whiteTex);

        // Gauge Fill depending on state
        if (state == GameRhythmState.Normal)
        {
            float maxCombo = Mathf.Max(1, BeatManager.Instance.ComboToTriggerToFever);
            float ratio = Mathf.Clamp01((float)BeatManager.Instance.NormalFeverProgress / maxCombo);
            GUI.color = new Color(0.15f, 0.85f, 1f, 0.95f); // Bright Cyan
            GUI.DrawTexture(new Rect(barX, gaugeY, barWidth * ratio, gaugeHeight), whiteTex);
        }
        else if (state == GameRhythmState.ToFever)
        {
            float maxHits = Mathf.Max(1, BeatManager.Instance.HitsToCompleteToFever);
            float ratio = Mathf.Clamp01((float)BeatManager.Instance.CurrentToFeverHits / maxHits);
            GUI.color = new Color(1f, 0.82f, 0.1f, 0.95f); // Bright Gold
            GUI.DrawTexture(new Rect(barX, gaugeY, barWidth * ratio, gaugeHeight), whiteTex);
        }
        else if (state == GameRhythmState.Fever)
        {
            float totalFever = Mathf.Max(1f, BeatManager.Instance.FeverDuration);
            float ratio = Mathf.Clamp01(BeatManager.Instance.StateTimeRemaining / totalFever);
            GUI.color = new Color(1f, 0.25f, 0.1f, 0.95f); // Blazing Red-Orange
            GUI.DrawTexture(new Rect(barX, gaugeY, barWidth * ratio, gaugeHeight), whiteTex);
        }

        // 2. METRONOME BAR
        // Dark background box
        GUI.color = new Color(0.08f, 0.08f, 0.12f, 0.85f);
        GUI.DrawTexture(new Rect(barX - 2, barY - 2, barWidth + 4, barHeight + 4), whiteTex);

        // Good Window Zone (Yellowish background)
        float secPerHalfBeat = BeatManager.Instance.SecPerBeat * 0.5f;
        if (secPerHalfBeat > 0f)
        {
            float goodNormalized = Mathf.Clamp01(BeatManager.Instance.GoodWindow / secPerHalfBeat);
            float goodWidth = goodNormalized * (barWidth / 2f) * 2f;
            GUI.color = new Color(1f, 0.85f, 0.2f, 0.25f);
            GUI.DrawTexture(new Rect(centerX - goodWidth / 2f, barY, goodWidth, barHeight), whiteTex);

            // Perfect Window Zone (Cyan background)
            float perfectNormalized = Mathf.Clamp01(BeatManager.Instance.PerfectWindow / secPerHalfBeat);
            float perfectWidth = perfectNormalized * (barWidth / 2f) * 2f;
            GUI.color = new Color(0.2f, 0.95f, 0.7f, 0.45f);
            GUI.DrawTexture(new Rect(centerX - perfectWidth / 2f, barY, perfectWidth, barHeight), whiteTex);
        }

        // Center Hit Line (Target)
        Color hitLineColor = Color.Lerp(new Color(1f, 1f, 1f, 0.85f), Color.yellow, beatFlashIntensity);
        GUI.color = hitLineColor;
        float hitLineWidth = 4f + (beatFlashIntensity * 4f);
        GUI.DrawTexture(new Rect(centerX - hitLineWidth / 2f, barY - 4, hitLineWidth, barHeight + 8), whiteTex);

        // Moving Beat Cursors approaching the center from left and right
        float signedOffset = BeatManager.Instance.GetSignedOffsetToNearestBeat();
        float halfBeat = BeatManager.Instance.SecPerBeat * 0.5f;

        if (halfBeat > 0f)
        {
            // progress: 1.0 at outer edge, 0.0 at center
            float distFromCenter = Mathf.Clamp(signedOffset / halfBeat, -1f, 1f);
            float markerOffsetPixels = distFromCenter * (barWidth / 2f);

            // Left approaching cursor
            float leftCursorX = centerX + markerOffsetPixels;
            // Right approaching cursor (mirrored)
            float rightCursorX = centerX - markerOffsetPixels;

            GUI.color = new Color(0f, 0.9f, 1f, 0.95f); // Bright cyan
            float cursorWidth = 5f;
            GUI.DrawTexture(new Rect(leftCursorX - cursorWidth / 2f, barY, cursorWidth, barHeight), whiteTex);
            GUI.DrawTexture(new Rect(rightCursorX - cursorWidth / 2f, barY, cursorWidth, barHeight), whiteTex);
        }

        // 3. Status / Calibration Text Below Bar
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 13,
            fontStyle = FontStyle.Bold
        };

        if (state == GameRhythmState.Fever)
        {
            labelStyle.normal.textColor = Color.red;
            GUI.Label(new Rect(barX - 100, barY + barHeight + 4, barWidth + 200, 24),
                $"🔥 FEVER ACTIVE: {BeatManager.Instance.StateTimeRemaining:F1}s | DOMINO CASCADE! 🔥", labelStyle);
        }
        else if (state == GameRhythmState.ToFever)
        {
            labelStyle.normal.textColor = Color.yellow;
            if (BeatManager.Instance.CurrentToFeverHits >= BeatManager.Instance.HitsToCompleteToFever)
            {
                GUI.Label(new Rect(barX - 150, barY + barHeight + 4, barWidth + 300, 24),
                    $"⚡ FEVER READY! ({BeatManager.Instance.StateTimeRemaining:F1}s Menuju Drop) | Offset: {BeatManager.Instance.CurrentSongOffset:+0.00;-0.00;0.00}s ⚡", labelStyle);
            }
            else
            {
                GUI.Label(new Rect(barX - 150, barY + barHeight + 4, barWidth + 300, 24),
                    $"⚡ TO FEVER: {BeatManager.Instance.CurrentToFeverHits}/{BeatManager.Instance.HitsToCompleteToFever} Hits ({BeatManager.Instance.StateTimeRemaining:F1}s) | Offset: {BeatManager.Instance.CurrentSongOffset:+0.00;-0.00;0.00}s ⚡", labelStyle);
            }
        }
        else
        {
            labelStyle.normal.textColor = new Color(0.85f, 0.85f, 0.95f);
            GUI.Label(new Rect(barX - 150, barY + barHeight + 4, barWidth + 300, 24),
                $"FEVER BAR: {BeatManager.Instance.NormalFeverProgress}/{BeatManager.Instance.ComboToTriggerToFever} | BPM: {BeatManager.Instance.Bpm:F0} | Offset: {BeatManager.Instance.CurrentSongOffset:+0.00;-0.00;0.00}s", labelStyle);
        }

        GUI.color = prevColor;
    }
}
