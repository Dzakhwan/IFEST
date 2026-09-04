using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private int spawnEveryNBeats = 6;
    [SerializeField] private bool autoSpawnOnBeat = true;

    private int lastSpawnedWaypoint = -1;

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

    /// <summary>
    /// Evaluates beat interval to spawn new enemies and ensures player waypoints are synced.
    /// </summary>
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

        if (beatCount > 0 && beatCount % spawnEveryNBeats == 0)
        {
            SpawnNextEnemy();
        }
    }

    /// <summary>
    /// Spawns an enemy at the next sequential waypoint, automatically skipping the waypoint currently occupied by the player.
    /// </summary>
    public void SpawnNextEnemy()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        if (playerMovement == null)
        {
            playerMovement = FindFirstObjectByType<PlayerMovement>();
        }

        int playerWaypoint = playerMovement != null ? playerMovement.GetCurrentWaypointIndex() : -1;
        int targetIndex = (lastSpawnedWaypoint + 1) % waypoints.Length;

        // Skip waypoint if player is currently on it (unless only 1 waypoint exists)
        if (waypoints.Length > 1 && targetIndex == playerWaypoint)
        {
            targetIndex = (targetIndex + 1) % waypoints.Length;
        }

        SpawnEnemyAtWaypoint(targetIndex);
    }

    /// <summary>
    /// Instantiates enemy prefab at specified waypoint index and registers it in turn queue.
    /// </summary>
    /// <param name="waypointIndex">Target waypoint index to spawn enemy.</param>
    public void SpawnEnemyAtWaypoint(int waypointIndex)
    {
        if (waypointIndex < 0 || waypointIndex >= waypoints.Length) return;

        Transform spawnPoint = waypoints[waypointIndex];
        GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPoint.position, Quaternion.identity);

        Enemy enemy = newEnemyObj.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.SetWaypointIndex(waypointIndex);
            enemy.SetActiveTarget(false); // Default to waiting line

            // Notify player combat to update queue if needed
            PlayerCombat combat = FindFirstObjectByType<PlayerCombat>();
            if (combat != null)
            {
                combat.EnsureActiveTarget();
            }

            Debug.Log($"[EnemySpawner] Spawned Waiting Enemy at Waypoint {waypointIndex}");
        }

        lastSpawnedWaypoint = waypointIndex;
    }
}
