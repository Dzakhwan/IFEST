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
    private float moveTimer = 0f;
    private const float MAX_MOVE_DURATION = 0.6f;

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

    /// <summary>
    /// Returns true if player sprite is currently flipped (facing left).
    /// </summary>
    public bool IsFacingLeft => spriteRenderer != null && spriteRenderer.flipX;

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
            spriteRenderer.flipX = true;
            MovePrevious();
        }
    }

    /// <summary>
    /// Finds any blocking enemy currently occupying the specified enemy slot A_k.
    /// In ABABABABA layout, slot A_k is located between player node B_k and B_{k+1}.
    /// </summary>
    /// <param name="slotIndex">The enemy slot index to check.</param>
    /// <returns>Enemy instance if found and blocking, null otherwise.</returns>
    public Enemy GetEnemyAtSlot(int slotIndex)
    {
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var enemy in enemies)
        {
            if (enemy != null && enemy.IsBlocking && enemy.WaypointIndex == slotIndex)
            {
                return enemy;
            }
        }
        return null;
    }

    /// <summary>
    /// Checks whether an enemy slot currently has a blocking enemy.
    /// </summary>
    public bool IsSlotOccupied(int slotIndex)
    {
        return GetEnemyAtSlot(slotIndex) != null;
    }

    public Enemy GetEnemyAtWaypoint(int index) => GetEnemyAtSlot(index);
    public bool IsWaypointOccupied(int index) => IsSlotOccupied(index);

    /// <summary>
    /// Moves player to the next player node B_{k+1} to the right.
    /// Checks enemy slot A_k between current node and next node.
    /// If slot has the Active Target (red), player stops and cannot pass.
    /// If slot is empty or has a waiting (gray) enemy, player advances to B_{k+1}.
    /// </summary>
    private void MoveNext()
    {
        if (waypoints.Length == 0) return;

        int candidate = currentWaypointIndex + 1;
        if (candidate >= waypoints.Length) return;

        // Slot A_k is between B_k (currentWaypointIndex) and B_{k+1} (candidate)
        Enemy enemy = GetEnemyAtSlot(currentWaypointIndex);
        if (enemy != null && enemy.IsActiveTarget)
        {
            // Red enemy blocks the path: player must defeat it first!
            return;
        }

        targetWaypointIndex = candidate;
        Debug.Log(
            $"NEXT: B{currentWaypointIndex} → B{targetWaypointIndex} | " +
            $"Current: {waypoints[currentWaypointIndex].position} | " +
            $"Target: {waypoints[targetWaypointIndex].position}"
        );
        moveTimer = 0f;
        isMoving = true;
        SetMovementAnimation(true);
    }

    /// <summary>
    /// Moves player to the previous player node B_{k-1} to the left.
    /// Checks enemy slot A_{k-1} between current node and previous node.
    /// If slot has the Active Target (red), player stops and cannot pass.
    /// If slot is empty or has a waiting (gray) enemy, player advances to B_{k-1}.
    /// </summary>
    private void MovePrevious()
    {
        if (waypoints.Length == 0) return;

        int candidate = currentWaypointIndex - 1;
        if (candidate < 0) return;

        // Slot A_{k-1} is between B_{k-1} (candidate) and B_k (currentWaypointIndex)
        Enemy enemy = GetEnemyAtSlot(candidate);
        if (enemy != null && enemy.IsActiveTarget)
        {
            // Red enemy blocks the path: player must defeat it first!
            return;
        }

        targetWaypointIndex = candidate;
        Debug.Log(
            $"PREV: B{currentWaypointIndex} → B{targetWaypointIndex} | " +
            $"Current: {waypoints[currentWaypointIndex].position} | " +
            $"Target: {waypoints[targetWaypointIndex].position}"
        );
        moveTimer = 0f;
        isMoving = true;
        SetMovementAnimation(true);
    }

    private void MoveToWaypoint()
    {
        if (!isMoving)
            return;

        moveTimer += Time.fixedDeltaTime;
        Vector2 targetPosition = waypoints[targetWaypointIndex].position;

        Vector2 newPosition = Vector2.MoveTowards(
            rb.position,
            targetPosition,
            moveSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(newPosition);

        bool reached = Vector2.Distance(newPosition, targetPosition) < 0.05f
                    || Vector2.Distance(rb.position, targetPosition) < 0.05f
                    || moveTimer >= MAX_MOVE_DURATION;

        if (reached)
        {
            rb.position = targetPosition;
            currentWaypointIndex = targetWaypointIndex;
            isMoving = false;
            moveTimer = 0f;
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
