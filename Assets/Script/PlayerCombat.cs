using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    // Events
    public event Action<HitRating, int, string> OnAttackExecuted; // rating, combo, customMessage

    private int comboCount = 0;

    private void Awake()
    {
        if (inputHandler == null)
            inputHandler = GetComponent<InputHandler>();
        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        UpdateTargetQueue();
    }

    private void OnEnable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnAttackInput += HandleAttack;
        }
    }

    private void OnDisable()
    {
        if (inputHandler != null)
        {
            inputHandler.OnAttackInput -= HandleAttack;
        }
    }

    private void Update()
    {
        // Ensure at least one enemy is active if queue has waiting enemies
        EnsureActiveTarget();
    }

    private void HandleAttack()
    {
        if (BeatManager.Instance == null)
        {
            Debug.LogWarning("[PlayerCombat] BeatManager Instance missing in scene!");
            return;
        }

        HitRating rating = BeatManager.Instance.EvaluateHitTiming();
        int currentWaypoint = playerMovement != null ? playerMovement.GetCurrentWaypointIndex() : 0;

        // Find active target enemy in line
        Enemy activeEnemy = GetActiveTargetEnemy();
        Enemy enemyAtWaypoint = FindEnemyAtWaypoint(currentWaypoint);

        if (rating != HitRating.Miss)
        {
            if (enemyAtWaypoint != null)
            {
                if (enemyAtWaypoint == activeEnemy && enemyAtWaypoint.IsActiveTarget)
                {
                    animator.SetTrigger("Attack");
                    bool success = enemyAtWaypoint.ProcessHit(rating);
                    if (success)
                    {
                        comboCount++;
                        BeatManager.Instance.SetCombo(comboCount);
                        Debug.Log($"<color=green>[1-HIT TURN KILL: {rating.ToString().ToUpper()}]</color> Combo x{comboCount}");
                        OnAttackExecuted?.Invoke(rating, comboCount, $"{rating.ToString().ToUpper()}!");

                        // Advance turn to next enemy in line
                        AdvanceToNextTarget();
                    }
                }
                else
                {
                    comboCount = 0;
                    BeatManager.Instance.SetCombo(0);
                    Debug.Log($"<color=red>[OUT OF TURN]</color> Enemy is waiting for its turn! Combo reset.");
                    OnAttackExecuted?.Invoke(HitRating.Miss, 0, "OUT OF TURN!");
                }
            }
            else
            {
                comboCount = 0;
                BeatManager.Instance.SetCombo(0);
                Debug.Log($"<color=orange>[WHIFF]</color> No enemy at waypoint {currentWaypoint}. Combo reset.");
                OnAttackExecuted?.Invoke(HitRating.Miss, 0, "NO TARGET!");
            }
        }
        else
        {
            comboCount = 0;
            BeatManager.Instance.SetCombo(0);
            Debug.Log($"<color=red>[RHYTHM MISS]</color> Off beat! Combo reset.");
            OnAttackExecuted?.Invoke(HitRating.Miss, 0, "MISS!");
        }
    }

    /// <summary>
    /// Ensures that exactly one enemy in line is activated for their turn.
    /// </summary>
    public void EnsureActiveTarget()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0) return;

        bool hasActive = false;
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.IsActiveTarget)
            {
                hasActive = true;
                break;
            }
        }

        if (!hasActive)
        {
            UpdateTargetQueue();
        }
    }

    /// <summary>
    /// Sets the first enemy in line (sorted by waypoint index) as the Active Target.
    /// </summary>
    public void UpdateTargetQueue()
    {
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (allEnemies.Length == 0) return;

        // Sort enemies by waypoint index
        List<Enemy> enemyList = new List<Enemy>(allEnemies);
        enemyList.Sort((a, b) => a.WaypointIndex.CompareTo(b.WaypointIndex));

        for (int i = 0; i < enemyList.Count; i++)
        {
            enemyList[i].SetActiveTarget(i == 0); // Only first enemy in line gets active turn
        }

        if (enemyList.Count > 0)
        {
            Debug.Log($"<color=yellow>[TURN STARTED]</color> Enemy at Waypoint {enemyList[0].WaypointIndex} is now ACTIVE TARGET.");
        }
    }

    private void AdvanceToNextTarget()
    {
        // Short delay to allow current enemy to destroy, then assign next target
        Invoke(nameof(UpdateTargetQueue), 0.15f);
    }

    private Enemy GetActiveTargetEnemy()
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsActiveTarget)
                return enemy;
        }
        return null;
    }

    private Enemy FindEnemyAtWaypoint(int waypointIndex)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.WaypointIndex == waypointIndex)
            {
                return enemy;
            }
        }
        return null;
    }

    public int ComboCount => comboCount;
}
