using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 5f;

    /// <summary>
    /// Gets the list of movement waypoints shared with EnemySpawner.
    /// </summary>
    public Transform[] Waypoints => waypoints;

    private int currentWaypointIndex = 0;
    private int targetWaypointIndex;
    private bool isMoving = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputHandler = GetComponent<InputHandler>();
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (waypoints.Length > 0)
        {
            rb.position = waypoints[0].position;
        }
    }

    private void Update()
    {
        HandleInput();
    }

    private void FixedUpdate()
    {
        MoveToWaypoint();
    }

    private void HandleInput()
    {
        if (isMoving || waypoints.Length == 0)
            return;

        Vector2 input = inputHandler.MovementInput;

        if (input.x > 0)
        {
            spriteRenderer.flipX = false;
            MoveNext();
        }
        else if (input.x < 0)
        {
            MovePrevious();
            spriteRenderer.flipX = true; // Flip sprite when moving left
        }
    }

    /// <summary>
    /// Checks whether an enemy is currently occupying the specified waypoint index.
    /// </summary>
    /// <param name="index">The waypoint index to check.</param>
    /// <returns>True if an enemy is standing on the waypoint, false otherwise.</returns>
    private bool IsWaypointOccupied(int index)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.WaypointIndex == index)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Moves player to the next available waypoint to the right, skipping any waypoints occupied by enemies.
    /// </summary>
    private void MoveNext()
    {
        if (waypoints.Length == 0) return;

        int candidate = currentWaypointIndex + 1;

        // Skip waypoints that are occupied by enemies
        while (candidate < waypoints.Length && IsWaypointOccupied(candidate))
        {
            candidate++;
        }

        if (candidate >= waypoints.Length)
            return;

        targetWaypointIndex = candidate;
        Debug.Log(
            $"NEXT: {currentWaypointIndex} → {targetWaypointIndex} | " +
            $"Current: {waypoints[currentWaypointIndex].position} | " +
            $"Target: {waypoints[targetWaypointIndex].position}"
        );
        isMoving = true;
        SetMovementAnimation(true);
    }

    /// <summary>
    /// Moves player to the previous available waypoint to the left, skipping any waypoints occupied by enemies.
    /// </summary>
    private void MovePrevious()
    {
        if (waypoints.Length == 0) return;

        int candidate = currentWaypointIndex - 1;

        // Skip waypoints that are occupied by enemies
        while (candidate >= 0 && IsWaypointOccupied(candidate))
        {
            candidate--;
        }

        if (candidate < 0)
            return;

        targetWaypointIndex = candidate;
        isMoving = true;
        SetMovementAnimation(true);
    }

    private void MoveToWaypoint()
    {
        if (!isMoving)
            return;

        Vector2 targetPosition = waypoints[targetWaypointIndex].position;



        Vector2 newPosition = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);

        if (Vector2.Distance(newPosition, targetPosition) < 0.01f)
        {
            rb.position = targetPosition;
            currentWaypointIndex = targetWaypointIndex;
            isMoving = false;
            SetMovementAnimation(false);
        }
    }

    private void SetMovementAnimation(bool moving)
    {
        if (animator != null)
            animator.SetBool("IsDash", moving);
    }

    public int GetCurrentWaypointIndex()
    {
        return currentWaypointIndex;
    }

    public bool IsMoving()
    {
        return isMoving;
    }
}