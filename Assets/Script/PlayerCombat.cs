using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Animator animator;

    [Header("Domino Chain Configuration (Fever Mode)")]
    [Tooltip("If true, domino chain only propagates through enemies strictly adjacent with no empty slot gaps.")]
    [SerializeField] private bool requireContiguousDomino = true;
    [Tooltip("Maximum number of waiting enemies that can be wiped in a single domino chain.")]
    [SerializeField] private int maxDominoChainCount = 3;

    [Header("Combat Audio Feedback")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip missSound;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;
    [SerializeField] private AudioSource audioSource;

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

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
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

        // Find active target enemy and adjacent enemy (distance == 1)
        Enemy activeEnemy = GetActiveTargetEnemy();
        Enemy targetEnemy = FindAdjacentEnemy(currentWaypoint, activeEnemy);

        if (rating != HitRating.Miss)
        {
            if (targetEnemy != null)
            {
                if (targetEnemy == activeEnemy && targetEnemy.IsActiveTarget)
                {
                    animator.SetTrigger("Attack");
                    bool success = targetEnemy.ProcessHit(rating);
                    if (success)
                    {
                        comboCount++;
                        BeatManager.Instance.SetCombo(comboCount);
                        PlayHitSound(rating);
                        Debug.Log($"<color=green>[1-HIT TURN KILL: {rating.ToString().ToUpper()}]</color> Combo x{comboCount}");
                        OnAttackExecuted?.Invoke(rating, comboCount, $"{rating.ToString().ToUpper()}!");

                        // Check ToFever progress
                        if (BeatManager.Instance.CurrentState == GameRhythmState.ToFever)
                        {
                            BeatManager.Instance.RegisterToFeverHit();
                        }
                        // Check trigger to ToFever from Normal mode (isolated Fever gauge)
                        else if (BeatManager.Instance.CurrentState == GameRhythmState.Normal)
                        {
                            BeatManager.Instance.AddNormalFeverProgress();
                        }
                        // Trigger Domino Chain Reaction in Fever mode
                        else if (BeatManager.Instance.IsFeverActive)
                        {
                            TriggerDominoChain(targetEnemy.WaypointIndex);
                        }

                        // Advance turn to next enemy in line
                        AdvanceToNextTarget();
                    }
                }
                else
                {
                    comboCount = 0;
                    BeatManager.Instance.SetCombo(0);
                    PlayMissSound();
                    if (BeatManager.Instance.CurrentState == GameRhythmState.Normal)
                    {
                        BeatManager.Instance.ResetNormalFeverProgress();
                    }
                    else if (BeatManager.Instance.CurrentState == GameRhythmState.ToFever)
                    {
                        BeatManager.Instance.ResetToFeverHitsOnMiss();
                    }
                    Debug.Log($"<color=red>[OUT OF TURN]</color> Enemy is waiting for its turn! Combo reset.");
                    OnAttackExecuted?.Invoke(HitRating.Miss, 0, "OUT OF TURN!");
                }
            }
            else
            {
                comboCount = 0;
                BeatManager.Instance.SetCombo(0);
                PlayMissSound();
                if (BeatManager.Instance.CurrentState == GameRhythmState.Normal)
                {
                    BeatManager.Instance.ResetNormalFeverProgress();
                }
                else if (BeatManager.Instance.CurrentState == GameRhythmState.ToFever)
                {
                    BeatManager.Instance.ResetToFeverHitsOnMiss();
                }
                Debug.Log($"<color=orange>[WHIFF]</color> No enemy adjacent to waypoint {currentWaypoint}. Combo reset.");
                OnAttackExecuted?.Invoke(HitRating.Miss, 0, "NO TARGET!");
            }
        }
        else
        {
            comboCount = 0;
            BeatManager.Instance.SetCombo(0);
            PlayMissSound();
            if (BeatManager.Instance.CurrentState == GameRhythmState.Normal)
            {
                BeatManager.Instance.ResetNormalFeverProgress();
            }
            else if (BeatManager.Instance.CurrentState == GameRhythmState.ToFever)
            {
                BeatManager.Instance.ResetToFeverHitsOnMiss();
            }
            Debug.Log($"<color=red>[RHYTHM MISS]</color> Off beat! Combo reset.");
            OnAttackExecuted?.Invoke(HitRating.Miss, 0, "MISS!");
        }
    }

    private void PlayHitSound(HitRating rating)
    {
        if (hitSound != null && audioSource != null)
        {
            audioSource.pitch = (rating == HitRating.Perfect) ? 1.05f : 0.92f;
            audioSource.PlayOneShot(hitSound, sfxVolume);
        }
    }

    private void PlayMissSound()
    {
        if (missSound != null && audioSource != null)
        {
            audioSource.pitch = 1.0f;
            audioSource.PlayOneShot(missSound, sfxVolume);
        }
    }

    /// <summary>
    /// Triggers a contiguous sequential domino explosion wiping out waiting enemies
    /// positioned behind the defeated red target in the player's facing direction.
    /// If requireContiguousDomino is true, any gap of empty slots terminates the chain.
    /// </summary>
    private void TriggerDominoChain(int redEnemySlot)
    {
        bool facingLeft = playerMovement != null && playerMovement.IsFacingLeft;
        Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);

        // Map blocking enemies by slot index
        Dictionary<int, Enemy> enemySlotMap = new Dictionary<int, Enemy>();
        foreach (var enemy in allEnemies)
        {
            if (enemy != null && enemy.IsBlocking)
            {
                enemySlotMap[enemy.WaypointIndex] = enemy;
            }
        }

        List<Enemy> dominoVictims = new List<Enemy>();

        if (requireContiguousDomino)
        {
            // Step slot-by-slot in player facing direction
            int step = facingLeft ? -1 : 1;
            int checkSlot = redEnemySlot + step;

            while (dominoVictims.Count < maxDominoChainCount)
            {
                if (enemySlotMap.TryGetValue(checkSlot, out Enemy victim) && victim != null)
                {
                    dominoVictims.Add(victim);
                    checkSlot += step;
                }
                else
                {
                    // Empty slot gap encountered: Domino chain stops!
                    break;
                }
            }
        }
        else
        {
            // Fallback: take up to maxDominoChainCount enemies behind
            List<Enemy> behindEnemies = new List<Enemy>();
            foreach (var enemy in allEnemies)
            {
                if (enemy == null || !enemy.IsBlocking) continue;

                if (facingLeft && enemy.WaypointIndex < redEnemySlot)
                {
                    behindEnemies.Add(enemy);
                }
                else if (!facingLeft && enemy.WaypointIndex > redEnemySlot)
                {
                    behindEnemies.Add(enemy);
                }
            }

            if (facingLeft)
            {
                behindEnemies.Sort((a, b) => b.WaypointIndex.CompareTo(a.WaypointIndex));
            }
            else
            {
                behindEnemies.Sort((a, b) => a.WaypointIndex.CompareTo(b.WaypointIndex));
            }

            int countToTake = Mathf.Min(behindEnemies.Count, maxDominoChainCount);
            for (int i = 0; i < countToTake; i++)
            {
                dominoVictims.Add(behindEnemies[i]);
            }
        }

        if (dominoVictims.Count > 0)
        {
            float delayStep = 0.08f;
            for (int i = 0; i < dominoVictims.Count; i++)
            {
                Enemy victim = dominoVictims[i];
                victim.TriggerDominoDeath(delayStep * (i + 1));
                comboCount++;
            }
            BeatManager.Instance.SetCombo(comboCount);
            Debug.Log($"<color=red>[DOMINO CASCADE!]</color> {dominoVictims.Count} contiguous enemies wiped in chain reaction! Combo x{comboCount}");
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

    /// <summary>
    /// Finds an adjacent enemy in the direction the player is currently facing.
    /// In ABABABABA layout, player at node B_k attacks:
    /// - Slot A_k (to the right, between B_k and B_{k+1}) when facing right.
    /// - Slot A_{k-1} (to the left, between B_{k-1} and B_k) when facing left.
    /// </summary>
    /// <param name="currentWaypoint">Player's current node index B_k.</param>
    /// <param name="activeEnemy">The currently active target enemy in turn queue.</param>
    /// <returns>Adjacent enemy in the facing slot if found, or null if none.</returns>
    private Enemy FindAdjacentEnemy(int currentWaypoint, Enemy activeEnemy)
    {
        int targetSlot = (playerMovement != null && playerMovement.IsFacingLeft) 
            ? currentWaypoint - 1 
            : currentWaypoint;

        if (playerMovement != null && playerMovement.Waypoints != null)
        {
            int totalSlots = playerMovement.Waypoints.Length - 1;
            if (targetSlot < 0 || targetSlot >= totalSlots)
                return null;
        }

        // Prioritize active target if it is at the facing slot
        if (activeEnemy != null && activeEnemy.WaypointIndex == targetSlot)
        {
            return activeEnemy;
        }

        // Check if any other enemy is at the facing slot
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.WaypointIndex == targetSlot)
            {
                return enemy;
            }
        }
        return null;
    }

    public int ComboCount => comboCount;
}
