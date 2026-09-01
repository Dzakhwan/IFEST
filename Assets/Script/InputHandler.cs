using UnityEngine;
using UnityEngine.InputSystem;


public class InputHandler : MonoBehaviour
{
    public Vector2 MovementInput;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnMove(InputValue value)
    {
        MovementInput = value.Get<Vector2>();
    }
    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            Debug.Log("Attack");
        }
    }
}
