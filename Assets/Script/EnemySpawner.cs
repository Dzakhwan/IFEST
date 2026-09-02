using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Configuration")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private int spawnEveryNBeats = 4;
    [SerializeField] private bool autoSpawnOnBeat = true;

    private int lastSpawnedWaypoint = -1;

    private void Start()
    {
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
        if (!autoSpawnOnBeat || enemyPrefab == null || waypoints == null || waypoints.Length == 0)
            return;

        if (beatCount > 0 && beatCount % spawnEveryNBeats == 0)
        {
            SpawnNextEnemy();
        }
    }

    public void SpawnNextEnemy()
    {
        int targetIndex = (lastSpawnedWaypoint + 1) % waypoints.Length;
        SpawnEnemyAtWaypoint(targetIndex);
    }

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
