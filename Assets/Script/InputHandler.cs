using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public Vector2 MovementInput { get; private set; }

    public event Action OnAttackInput;
    public event Action<Vector2> OnMoveInput;

    public void OnMove(InputValue value)
    {
        MovementInput = value.Get<Vector2>();
        OnMoveInput?.Invoke(MovementInput);
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Attack");
            OnAttackInput?.Invoke();
        }
    }
}

