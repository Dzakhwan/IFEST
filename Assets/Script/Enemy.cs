using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Attributes")]
    [SerializeField] private int waypointIndex = 1;
    [SerializeField] private bool isActiveTarget = false;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color activeTargetColor = Color.red;
    [SerializeField] private Color waitingColor = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Dimmed waiting turn
    [SerializeField] private Color hitFlashColor = Color.yellow;

    private Vector3 originalScale;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        originalScale = transform.localScale;
        UpdateVisuals();
    }

    private void Start()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat += OnBeatPulse;
        }
    }

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= OnBeatPulse;
        }
    }

    public void SetActiveTarget(bool active)
    {
        isActiveTarget = active;
        UpdateVisuals();
    }

    public void SetWaypointIndex(int index)
    {
        waypointIndex = index;
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isActiveTarget ? activeTargetColor : waitingColor;
        }
    }

    private void OnBeatPulse(int beat)
    {
        // Pulse only if active target or subtle pulse
        if (isActiveTarget)
        {
            StartCoroutine(PulseRoutine());
        }
    }

    private IEnumerator PulseRoutine()
    {
        transform.localScale = originalScale * 1.2f;
        float elapsed = 0f;
        float duration = 0.1f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, elapsed / duration);
            yield return null;
        }
        transform.localScale = originalScale;
    }

    /// <summary>
    /// Processes a 1-hit rhythm hit attempt on this enemy.
    /// Returns true if hit was valid (Active target hit on beat), false if out of turn.
    /// </summary>
    public bool ProcessHit(HitRating rating)
    {
        if (isActiveTarget)
        {
            Debug.Log($"<color=gold>[1-HIT KILL!]</color> Active Target Enemy at Waypoint {waypointIndex} defeated ({rating})!");
            StartCoroutine(FlashAndDieRoutine());
            return true;
        }
        else
        {
            Debug.Log($"<color=red>[OUT OF TURN!]</color> Hit waiting enemy at Waypoint {waypointIndex} before its turn!");
            StartCoroutine(FlashPenaltyRoutine());
            return false;
        }
    }

    private IEnumerator FlashAndDieRoutine()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    private IEnumerator FlashPenaltyRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.magenta;
            yield return new WaitForSeconds(0.2f);
            UpdateVisuals();
        }
    }

    public bool IsActiveTarget => isActiveTarget;
    public int WaypointIndex => waypointIndex;
}
