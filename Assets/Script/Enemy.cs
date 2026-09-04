using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Enemy Attributes")]
    [SerializeField] private int waypointIndex = 1;
    [SerializeField] private bool isActiveTarget = false;

    [Header("Despawn")]
    [SerializeField] private int maxAliveBeat = 12; // beats before self-destruct
    private int beatsAlive = 0;

    [Header("Visual Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color activeTargetColor = Color.red;
    [SerializeField] private Color waitingColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
    [SerializeField] private Color hitFlashColor = Color.yellow;

    private Vector3 originalScale;
    private Collider2D col;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

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
        beatsAlive++;

        if (beatsAlive >= maxAliveBeat)
        {
            StartCoroutine(DespawnRoutine());
            return;
        }

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
        // Disable collider immediately so the waypoint opens up for the player
        SetColliderEnabled(false);

        if (spriteRenderer != null)
            spriteRenderer.color = hitFlashColor;

        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    /// <summary>
    /// Triggered when this enemy is hit by a Domino Chain Reaction during Fever mode.
    /// </summary>
    public void TriggerDominoDeath(float delay)
    {
        SetColliderEnabled(false);
        StartCoroutine(DominoExplodeRoutine(delay));
    }

    private IEnumerator DominoExplodeRoutine(float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        if (spriteRenderer != null)
            spriteRenderer.color = new Color(1f, 0.3f, 0f); // Fiery orange-red explosion color

        // Pop scale burst effect
        transform.localScale = originalScale * 1.4f;

        Debug.Log($"<color=red>[DOMINO EXPLOSION!]</color> Enemy at Waypoint {waypointIndex} wiped by chain reaction!");
        yield return new WaitForSeconds(0.12f);
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

    private IEnumerator DespawnRoutine()
    {
        SetColliderEnabled(false);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(1f, 0.5f, 0f);
            yield return new WaitForSeconds(0.2f);
        }
        Destroy(gameObject);
    }

    private void SetColliderEnabled(bool enabled)
    {
        if (col != null)
            col.enabled = enabled;
    }

    public bool IsActiveTarget => isActiveTarget;
    public int WaypointIndex => waypointIndex;

    /// <summary>
    /// True while this enemy physically blocks its waypoint (collider enabled).
    /// False during hit/despawn flash so the player can pass through immediately.
    /// </summary>
    public bool IsBlocking => col != null && col.enabled;
}
