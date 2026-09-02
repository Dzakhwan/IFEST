using System.Collections;
using UnityEngine;

public class RhythmUI : MonoBehaviour
{
    [Header("UI Preferences")]
    [SerializeField] private bool useOnGUIDebugUI = true;

    private string lastRatingText = "";
    private Color ratingColor = Color.white;
    private int currentCombo = 0;
    private float displayTimer = 0f;

    private void Start()
    {
        PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
        if (combat != null)
        {
            combat.OnAttackExecuted += OnAttackResult;
        }
    }

    private void OnAttackResult(HitRating rating, int combo, string customMessage)
    {
        currentCombo = combo;
        lastRatingText = !string.IsNullOrEmpty(customMessage) ? customMessage : rating.ToString().ToUpper() + "!";
        displayTimer = 1.2f;

        if (customMessage.Contains("OUT OF TURN") || customMessage.Contains("MISS"))
        {
            ratingColor = Color.red;
        }
        else if (customMessage.Contains("NO TARGET"))
        {
            ratingColor = Color.yellow;
        }
        else
        {
            switch (rating)
            {
                case HitRating.Perfect:
                    ratingColor = Color.yellow;
                    break;
                case HitRating.Good:
                    ratingColor = Color.green;
                    break;
                default:
                    ratingColor = Color.red;
                    break;
            }
        }

        StopAllCoroutines();
        StartCoroutine(ClearTextRoutine());
    }

    private IEnumerator ClearTextRoutine()
    {
        while (displayTimer > 0)
        {
            displayTimer -= Time.deltaTime;
            yield return null;
        }
        lastRatingText = "";
    }

    private void OnGUI()
    {
        if (!useOnGUIDebugUI) return;

        // Visual Beat Metronome Indicator (Top Right)
        if (BeatManager.Instance != null)
        {
            float progress = BeatManager.Instance.GetBeatProgress();
            GUIStyle beatStyle = new GUIStyle(GUI.skin.box);
            beatStyle.fontSize = 14;
            beatStyle.normal.textColor = Color.cyan;

            GUI.Box(new Rect(Screen.width - 240, 20, 220, 55), 
                $"BPM: {BeatManager.Instance.Bpm}\nBeat: {BeatManager.Instance.CurrentBeat}");

            // Pulsing beat bar
            float barWidth = Mathf.Lerp(10, 200, progress);
            Texture2D texture = Texture2D.whiteTexture;
            GUI.color = Color.Lerp(Color.yellow, Color.cyan, progress);
            GUI.DrawTexture(new Rect(Screen.width - 230, 58, barWidth, 8), texture);
            GUI.color = Color.white;
        }

        // Hit Rating Display (Center Screen)
        if (!string.IsNullOrEmpty(lastRatingText))
        {
            GUIStyle ratingStyle = new GUIStyle(GUI.skin.label);
            ratingStyle.alignment = TextAnchor.MiddleCenter;
            ratingStyle.fontSize = 38;
            ratingStyle.fontStyle = FontStyle.Bold;
            ratingStyle.normal.textColor = ratingColor;

            GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 100, 400, 60), lastRatingText, ratingStyle);

            if (currentCombo > 1)
            {
                GUIStyle comboStyle = new GUIStyle(GUI.skin.label);
                comboStyle.alignment = TextAnchor.MiddleCenter;
                comboStyle.fontSize = 26;
                comboStyle.normal.textColor = Color.yellow;

                GUI.Label(new Rect(Screen.width / 2f - 200, Screen.height / 2f - 40, 400, 40), $"COMBO x{currentCombo}", comboStyle);
            }
        }
    }
}
