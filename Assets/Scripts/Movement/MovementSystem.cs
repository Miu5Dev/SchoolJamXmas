using System;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    private Vector2 InputDirection;
    [SerializeField]private int moveSpeed;
    
    [SerializeField]private bool running = false;
    [SerializeField]private bool jumping = false;
    [SerializeField]private bool crouching = false;
    
    void OnEnable()
    {
        // Subscribe to movement events
        EventBus.Subscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Subscribe<onCrouchInputEvent>(OnCrouchInput);
        EventBus.Subscribe<onRunInputEvent>(OnRunInput);
        
        Debug.Log("STARTED");
        
    }
    
    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Unsubscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Unsubscribe<onCrouchInputEvent>(OnCrouchInput);
        EventBus.Unsubscribe<onRunInputEvent>(OnRunInput);
    }

    private void OnMoveInput(onMoveInputEvent ev)
    {
        InputDirection = ev.Direction;
        Debug.Log(InputDirection);
    }

    private void OnRunInput(onRunInputEvent ev)
    {
        running = ev.pressed;
    }

    private void OnJumpInput(onJumpInputEvent ev)
    {
        jumping = ev.pressed;
    }

    private void OnCrouchInput(onCrouchInputEvent ev)
    {
        crouching = ev.pressed;
    }

    private void FixedUpdate()
    {
        if (InputDirection.magnitude > 0)
        {
            Vector2 moveVector = InputDirection * (moveSpeed * Time.deltaTime);
            transform.position += new Vector3(moveVector.x, 0, moveVector.y);
        }
    }
}
