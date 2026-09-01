using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private InputHandler inputHandler;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 5f;

    private int currentWaypointIndex = 0;
    private int targetWaypointIndex;
    private bool isMoving = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputHandler = GetComponent<InputHandler>();

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
            MoveNext();
        }
        else if (input.x < 0)
        {
            MovePrevious();
        }
    }

    private void MoveNext()
    {
        if (currentWaypointIndex >= waypoints.Length - 1)
            return;

        targetWaypointIndex = currentWaypointIndex + 1;
        Debug.Log(
        $"NEXT: {currentWaypointIndex} → {targetWaypointIndex} | " +
        $"Current: {waypoints[currentWaypointIndex].position} | " +
        $"Target: {waypoints[targetWaypointIndex].position}"
    );
        isMoving = true;
    }

    private void MovePrevious()
    {
        if (currentWaypointIndex <= 0)
            return;

        targetWaypointIndex = currentWaypointIndex - 1;
        isMoving = true;
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
        }
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