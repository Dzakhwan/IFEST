using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform[] waypoints; // Player B nodes
    [SerializeField] private Transform[] customEnemySlots; // Optional custom A slots
    [SerializeField] private bool autoSpawnOnBeat = true;
    [SerializeField] private int maxEnemies = 6;

    [Header("Adaptive Spawn Rates (Per State)")]
    [Tooltip("Normal mode spawn interval in beats (Default: 2 beats = 2x faster than 4 beats).")]
    [SerializeField] private int normalSpawnEveryNBeats = 2;
    [Tooltip("To Fever mode spawn interval in beats (Default: 2 beats).")]
    [SerializeField] private int toFeverSpawnEveryNBeats = 2;
    [Tooltip("Fever mode spawn interval in beats (Default: 1 beat = 4x faster than 4 beats).")]
    [SerializeField] private int feverSpawnEveryNBeats = 1;
    [Tooltip("In Fever mode, rapidly spawn an extra enemy if count drops below 3 after domino wipe.")]
    [SerializeField] private bool feverRapidFill = true;

    // Legacy field preserved for Inspector backwards-compatibility
    [HideInInspector][SerializeField] private int spawnEveryNBeats = 2;

    public int TotalEnemySlots
    {
        get
        {
            if (customEnemySlots != null && customEnemySlots.Length > 0)
                return customEnemySlots.Length;
            if (waypoints != null && waypoints.Length > 1)
                return waypoints.Length - 1;
            return 0;
        }
    }

    /// <summary>
    /// Gets the world position for enemy slot A_k.
    /// Uses customEnemySlots if assigned, otherwise automatically computes the midpoint between player nodes B_k and B_{k+1}.
    /// </summary>
    public Vector3 GetSlotPosition(int slotIndex)
    {
        if (customEnemySlots != null && slotIndex >= 0 && slotIndex < customEnemySlots.Length)
        {
            if (customEnemySlots[slotIndex] != null)
                return customEnemySlots[slotIndex].position;
        }

        if (waypoints != null && slotIndex >= 0 && slotIndex < waypoints.Length - 1)
        {
            if (waypoints[slotIndex] != null && waypoints[slotIndex + 1] != null)
            {
                return (waypoints[slotIndex].position + waypoints[slotIndex + 1].position) * 0.5f;
            }
        }

        return Vector3.zero;
    }

    private void Start()
    {
        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        if ((waypoints == null || waypoints.Length == 0) && playerMovement != null)
        {
            waypoints = playerMovement.Waypoints;
        }

        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat += OnBeatCheck;
        }
    }

    private void OnDestroy()
    {
        if (BeatManager.Instance != null)
        {
            BeatManager.Instance.OnBeat -= OnBeatCheck;
        }
    }

    private void OnBeatCheck(int beatCount)
    {
        if (!autoSpawnOnBeat || enemyPrefab == null)
            return;

        if (waypoints == null || waypoints.Length == 0)
        {
            if (playerMovement != null && playerMovement.Waypoints != null && playerMovement.Waypoints.Length > 0)
            {
                waypoints = playerMovement.Waypoints;
            }
            else
            {
                return;
            }
        }

        // Determine active spawn rate based on current rhythm state
        int interval = normalSpawnEveryNBeats;
        bool isFever = false;

        if (BeatManager.Instance != null)
        {
            if (BeatManager.Instance.IsFeverActive)
            {
                interval = feverSpawnEveryNBeats;
                isFever = true;
            }
            else if (BeatManager.Instance.CurrentState == GameRhythmState.ToFever)
            {
                interval = toFeverSpawnEveryNBeats;
            }
        }

        if (interval <= 0) interval = 1;

        if (beatCount > 0 && beatCount % interval == 0)
        {
            SpawnNextEnemy();

            // Fever rapid refill: if domino cascade wiped out almost all enemies, fill an extra slot
            if (isFever && feverRapidFill)
            {
                Enemy[] remaining = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
                if (remaining.Length < 3)
                {
                    SpawnNextEnemy();
                }
            }
        }
    }

    /// <summary>
    /// Spawns an enemy at a valid A slot, respecting max count and anti-corner-trap rules.
    /// </summary>
    public void SpawnNextEnemy()
    {
        int totalSlots = TotalEnemySlots;
        if (totalSlots == 0) return;

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        // Cap: don't spawn if at max enemies
        Enemy[] existingEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        if (existingEnemies.Length >= maxEnemies)
        {
            Debug.Log($"[EnemySpawner] Max enemies ({maxEnemies}) reached. Skipping spawn.");
            return;
        }

        int playerNode = playerMovement != null ? playerMovement.GetCurrentWaypointIndex() : 0;

        // Build occupied slots set
        HashSet<int> occupiedSlots = new HashSet<int>();
        foreach (var enemy in existingEnemies)
        {
            if (enemy != null)
                occupiedSlots.Add(enemy.WaypointIndex);
        }

        // Find free slots
        List<int> freeSlots = new List<int>();
        for (int i = 0; i < totalSlots; i++)
        {
            if (!occupiedSlots.Contains(i))
                freeSlots.Add(i);
        }

        if (freeSlots.Count == 0)
        {
            Debug.Log("[EnemySpawner] No free enemy slots. Skipping spawn.");
            return;
        }

        // Anti-corner-trap: avoid spawning at the only exit slot if player is in a corner
        List<int> safeSlots = new List<int>();
        foreach (int slot in freeSlots)
        {
            if (!WouldTrapPlayer(slot, playerNode, totalSlots))
            {
                safeSlots.Add(slot);
            }
        }

        List<int> pool = safeSlots.Count > 0 ? safeSlots : freeSlots;
        int targetSlot = pool[Random.Range(0, pool.Count)];
        SpawnEnemyAtSlot(targetSlot);
    }

    private bool WouldTrapPlayer(int candidateSlot, int playerNode, int totalSlots)
    {
        // If player is at B0, the only exit is slot A0. Don't trap player in corner if other slots exist
        if (playerNode == 0 && candidateSlot == 0 && totalSlots > 1)
            return true;

        // If player is at B_last, the only exit is slot A_{totalSlots-1}. Don't trap player in corner
        int lastPlayerNode = (waypoints != null) ? waypoints.Length - 1 : totalSlots;
        if (playerNode == lastPlayerNode && candidateSlot == totalSlots - 1 && totalSlots > 1)
            return true;

        return false;
    }

    /// <summary>
    /// Instantiates enemy prefab at specified A slot index.
    /// </summary>
    /// <param name="slotIndex">Target enemy slot index A_k.</param>
    public void SpawnEnemyAtSlot(int slotIndex)
    {
        int totalSlots = TotalEnemySlots;
        if (slotIndex < 0 || slotIndex >= totalSlots) return;

        Vector3 spawnPosition = GetSlotPosition(slotIndex);
        GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        Enemy enemy = newEnemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetWaypointIndex(slotIndex); // Stores A slot index

            Enemy[] allEnemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
            bool hasActiveTarget = false;
            foreach (var existingEnemy in allEnemies)
            {
                if (existingEnemy != null && existingEnemy.IsActiveTarget)
                {
                    hasActiveTarget = true;
                    break;
                }
            }

            enemy.SetActiveTarget(!hasActiveTarget); // First enemy becomes active target when queue is empty

            // Notify player combat to update queue if needed
            PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
            if (combat != null)
            {
                combat.EnsureActiveTarget();
            }

            Debug.Log($"[EnemySpawner] Spawned Enemy at Slot A{slotIndex} at {spawnPosition} | Active={enemy.IsActiveTarget}");
        }
    }

    public void SpawnEnemyAtWaypoint(int waypointIndex) => SpawnEnemyAtSlot(waypointIndex);
}
