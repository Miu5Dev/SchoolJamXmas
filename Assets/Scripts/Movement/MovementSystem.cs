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
        EventBus.Subscribe<onMoveInputEvent>(onPlayerMove);
        EventBus.Subscribe<onJumpInputEvent>(onPlayerJump);
        EventBus.Subscribe<onCrouchInputEvent>(onPlayerCrouch);
        EventBus.Subscribe<onRunInputEvent>(onPlayerRun);
        
        Debug.Log("STARTED");
        
    }
    
    void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        EventBus.Unsubscribe<onMoveInputEvent>(onPlayerMove);
        EventBus.Unsubscribe<onJumpInputEvent>(onPlayerJump);
        EventBus.Unsubscribe<onCrouchInputEvent>(onPlayerCrouch);
        EventBus.Unsubscribe<onRunInputEvent>(onPlayerRun);
    }

    private void onPlayerMove(onMoveInputEvent ev)
    {
        InputDirection = ev.Direction;
        Debug.Log(InputDirection);
    }

    private void onPlayerRun(onRunInputEvent ev)
    {
        running = ev.pressed;
    }

    private void onPlayerJump(onJumpInputEvent ev)
    {
        jumping = ev.pressed;
    }

    private void onPlayerCrouch(onCrouchInputEvent ev)
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
