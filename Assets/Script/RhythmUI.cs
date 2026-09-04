using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RhythmUI : MonoBehaviour
{
    [Header("Rating Sprites (Left & Right Pairing)")]
    [Tooltip("Index 0 = Left Sprite, Index 1 = Right Sprite")]
    [SerializeField] private Sprite[] perfectSprites = new Sprite[2];
    [SerializeField] private Sprite[] goodSprites = new Sprite[2];
    [SerializeField] private Sprite[] missSprites = new Sprite[2];

    [Header("Hit Rating Display References")]
    [SerializeField] private Image ratingImageDisplay;        // Single display with dynamic position offset
    [SerializeField] private Image ratingImageDisplayLeft;    // Optional explicit left display
    [SerializeField] private Image ratingImageDisplayRight;   // Optional explicit right display
    [SerializeField] private float ratingOffsetX = 250f;      // Horizontal offset from center when using single display

    [Header("Combo UI Tier Settings")]
    [SerializeField] private Image comboImageDisplay;         // Canvas Image for Combo Banner/Badge
    [SerializeField] private TextMeshProUGUI comboTextTMP;    // TextMeshPro component inside/overlaying combo sprite
    [SerializeField] private Text comboTextLegacy;            // Standard UI Text fallback
    [SerializeField] private Sprite comboTier1Sprite;         // Combo < 20
    [SerializeField] private Sprite comboTier2Sprite;         // Combo 20 - 49
    [SerializeField] private Sprite comboTier3Sprite;         // Combo >= 50

    [Header("Combo Font Colors & Size Settings")]
    [SerializeField] private Color comboTier1Color = new Color(0.12f, 0.65f, 0.95f, 1f);   // Blue (<20 combo)
    [SerializeField] private Color comboTier2Color = new Color(0.98f, 0.85f, 0.05f, 1f);   // Yellow (20-49 combo)
    [SerializeField] private Color comboTier3Color = new Color(0.98f, 0.18f, 0.42f, 1f);   // Red/Pink (>=50 combo)
    [SerializeField] private float comboFontSize = 42f;                                     // Default font size
    [SerializeField] private bool useAutoFontSize = true;                                   // Enable TextMeshPro Auto Sizing (24-54pt)

    [Header("Combo Font Shadow Settings")]
    [SerializeField] private bool enableTextShadow = true;                                  // Add drop shadow to combo text
    [SerializeField] private Color textShadowColor = new Color(0f, 0f, 0f, 0.9f);         // Shadow color (Black)
    [SerializeField] private Vector2 textShadowOffset = new Vector2(2.5f, -2.5f);         // Shadow offset (X, Y)

    [Header("Combo Text Format Settings")]
    [SerializeField] private string comboTextFormat = "{0}X";                               // Text format (e.g. "5X", "25X", "50X")

    [Header("Visual Metronome Settings")]
    [SerializeField] private bool showVisualMetronome = true;
    [SerializeField] private bool useOnGUIDebugUI = false;

    private PlayerMovement playerMovement;
    private Sprite currentRatingSprite;
    private int currentCombo = 0;
    private float displayTimer = 0f;
    private Coroutine comboPunchCoroutine;
    private Coroutine ratingPopupCoroutine;
    private Vector3 comboOriginalScale = Vector3.one;

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

        if (comboImageDisplay != null)
        {
            comboOriginalScale = comboImageDisplay.transform.localScale;
        }
    }

    private void Start()
    {
        playerMovement = FindFirstObjectByType<PlayerMovement>();

        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null)
        {
            combat.OnAttackExecuted += OnAttackResult;
        }

        ClearRatingDisplay();
        UpdateComboUI(0);

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
        UpdateComboUI(combo);

        // Ignore NO TARGET messages (do not display any rating popup)
        if (!string.IsNullOrEmpty(customMessage) && customMessage.Contains("NO TARGET"))
        {
            ClearRatingDisplay();
            return;
        }

        // Determine Player Position Side: Left vs Right
        bool isPlayerOnLeftSide = IsPlayerOnLeftSide();

        // Select correct sprite:
        // When player is on LEFT -> show RIGHT rating sprite (Index 1) on RIGHT side of screen
        // When player is on RIGHT -> show LEFT rating sprite (Index 0) on LEFT side of screen
        int spriteIndex = isPlayerOnLeftSide ? 1 : 0;

        Sprite selectedSprite = null;

        if (customMessage.Contains("OUT OF TURN") || customMessage.Contains("MISS") || rating == HitRating.Miss)
        {
            selectedSprite = GetRatingSprite(missSprites, spriteIndex);
        }
        else if (rating == HitRating.Perfect)
        {
            selectedSprite = GetRatingSprite(perfectSprites, spriteIndex);
        }
        else if (rating == HitRating.Good)
        {
            selectedSprite = GetRatingSprite(goodSprites, spriteIndex);
        }

        currentRatingSprite = selectedSprite;
        displayTimer = 1.2f;

        // Display Rating on UI Canvas
        ShowRatingUI(selectedSprite, isPlayerOnLeftSide);

        if (ratingPopupCoroutine != null)
        {
            StopCoroutine(ratingPopupCoroutine);
        }
        ratingPopupCoroutine = StartCoroutine(ClearRatingDisplayRoutine());
    }

    private bool IsPlayerOnLeftSide()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if (playerMovement != null)
        {
            // Check player transform position X relative to center (0)
            return playerMovement.transform.position.x < 0f;
        }
        return true;
    }

    private Sprite GetRatingSprite(Sprite[] spriteArray, int index)
    {
        if (spriteArray == null || spriteArray.Length == 0) return null;
        if (index >= 0 && index < spriteArray.Length && spriteArray[index] != null)
        {
            return spriteArray[index];
        }
        // Fallback to index 0 if target index is null
        return spriteArray[0];
    }

    private void ShowRatingUI(Sprite sprite, bool isPlayerOnLeft)
    {
        if (sprite == null)
        {
            ClearRatingDisplay();
            return;
        }

        // Explicit dual displays (Left & Right)
        if (ratingImageDisplayLeft != null && ratingImageDisplayRight != null)
        {
            if (isPlayerOnLeft)
            {
                // Player on Left -> Show rating on Right
                ratingImageDisplayLeft.gameObject.SetActive(false);
                ratingImageDisplayRight.sprite = sprite;
                ratingImageDisplayRight.gameObject.SetActive(true);
            }
            else
            {
                // Player on Right -> Show rating on Left
                ratingImageDisplayRight.gameObject.SetActive(false);
                ratingImageDisplayLeft.sprite = sprite;
                ratingImageDisplayLeft.gameObject.SetActive(true);
            }
            return;
        }

        // Single display with dynamic anchored position
        if (ratingImageDisplay != null)
        {
            ratingImageDisplay.sprite = sprite;
            RectTransform rect = ratingImageDisplay.rectTransform;
            if (rect != null)
            {
                Vector2 pos = rect.anchoredPosition;
                pos.x = isPlayerOnLeft ? Mathf.Abs(ratingOffsetX) : -Mathf.Abs(ratingOffsetX);
                rect.anchoredPosition = pos;
            }
            ratingImageDisplay.gameObject.SetActive(true);
        }
    }

    private void ClearRatingDisplay()
    {
        currentRatingSprite = null;
        displayTimer = 0f;

        if (ratingImageDisplay != null)
            ratingImageDisplay.gameObject.SetActive(false);
        if (ratingImageDisplayLeft != null)
            ratingImageDisplayLeft.gameObject.SetActive(false);
        if (ratingImageDisplayRight != null)
            ratingImageDisplayRight.gameObject.SetActive(false);
    }

    private IEnumerator ClearRatingDisplayRoutine()
    {
        while (displayTimer > 0)
        {
            displayTimer -= Time.unscaledDeltaTime;
            yield return null;
        }
        ClearRatingDisplay();
    }

    private void UpdateComboUI(int combo)
    {
        if (combo <= 0)
        {
            if (comboImageDisplay != null)
                comboImageDisplay.gameObject.SetActive(false);
            return;
        }

        if (comboImageDisplay == null) return;

        // Select Combo Tier Sprite & Font Color:
        // Tier 1: combo < 20 (Blue)
        // Tier 2: 20 <= combo < 50 (Yellow)
        // Tier 3: combo >= 50 (Red/Pink)
        Sprite selectedComboSprite = null;
        Color targetFontColor = comboTier1Color;

        if (combo < 20)
        {
            selectedComboSprite = comboTier1Sprite;
            targetFontColor = comboTier1Color;
        }
        else if (combo < 50)
        {
            selectedComboSprite = comboTier2Sprite != null ? comboTier2Sprite : comboTier1Sprite;
            targetFontColor = comboTier2Color;
        }
        else
        {
            selectedComboSprite = comboTier3Sprite != null ? comboTier3Sprite : (comboTier2Sprite != null ? comboTier2Sprite : comboTier1Sprite);
            targetFontColor = comboTier3Color;
        }

        if (selectedComboSprite != null)
        {
            comboImageDisplay.sprite = selectedComboSprite;
        }

        // Update Combo Text (1X, 2X, 5X...) & apply tier font color + size + shadow
        string comboStr = string.Format(comboTextFormat, combo);
        if (comboTextTMP != null)
        {
            comboTextTMP.color = targetFontColor;
            comboTextTMP.alignment = TextAlignmentOptions.Center;
            if (useAutoFontSize)
            {
                comboTextTMP.enableAutoSizing = true;
                comboTextTMP.fontSizeMin = 24f;
                comboTextTMP.fontSizeMax = 54f;
            }
            else
            {
                comboTextTMP.enableAutoSizing = false;
                comboTextTMP.fontSize = comboFontSize;
            }
            comboTextTMP.text = comboStr;
            ApplyTextShadow(comboTextTMP);
        }
        if (comboTextLegacy != null)
        {
            comboTextLegacy.color = targetFontColor;
            comboTextLegacy.alignment = TextAnchor.MiddleCenter;
            comboTextLegacy.fontSize = Mathf.RoundToInt(comboFontSize);
            comboTextLegacy.text = comboStr;
            ApplyTextShadow(comboTextLegacy);
        }

        comboImageDisplay.gameObject.SetActive(true);

        // Scale punch animation for juicy feel
        if (comboPunchCoroutine != null)
        {
            StopCoroutine(comboPunchCoroutine);
        }
        comboPunchCoroutine = StartCoroutine(ComboPunchRoutine());
    }

    private void ApplyTextShadow(Graphic textGraphic)
    {
        if (textGraphic == null) return;
        Shadow shadow = textGraphic.GetComponent<Shadow>();
        if (enableTextShadow)
        {
            if (shadow == null)
            {
                shadow = textGraphic.gameObject.AddComponent<Shadow>();
            }
            shadow.effectColor = textShadowColor;
            shadow.effectDistance = textShadowOffset;
            shadow.enabled = true;
        }
        else if (shadow != null)
        {
            shadow.enabled = false;
        }
    }

    private IEnumerator ComboPunchRoutine()
    {
        if (comboImageDisplay == null) yield break;

        Transform t = comboImageDisplay.transform;
        Vector3 targetScale = comboOriginalScale * 1.3f;
        t.localScale = targetScale;

        float elapsed = 0f;
        float duration = 0.15f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(targetScale, comboOriginalScale, elapsed / duration);
            yield return null;
        }

        t.localScale = comboOriginalScale;
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
