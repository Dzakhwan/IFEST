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

    [Header("UI Preferences")]
    [SerializeField] private bool useOnGUIDebugUI = false;

    private Sprite currentRatingSprite;
    private int currentCombo = 0;
    private float displayTimer = 0f;

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
        if (!useOnGUIDebugUI) return;

        // Fallback OnGUI rendering if ratingImageDisplay is not assigned
        if (ratingImageDisplay == null && currentRatingSprite != null && currentRatingSprite.texture != null)
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
}

