using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles all input and raises events through the EventBus.
/// Fixed to properly support controllers with continuous input.
/// </summary>
public class InputSystem : MonoBehaviour
{
    private MyInputs inputs;
    
    // Store current input values for continuous polling
    private Vector2 currentMoveInput;
    private Vector2 currentLookInput;
    private bool isMoving;
    private bool isLooking;

    private void Awake()
    {
        inputs = new MyInputs();

        // Movement - track both performed and canceled
        inputs.Player.Move.performed += OnMovePerformed;
        inputs.Player.Move.canceled += OnMoveCanceled;
        
        // Look - track both performed and canceled
        inputs.Player.Look.performed += OnLookPerformed;
        inputs.Player.Look.canceled += OnLookCanceled;
        
        // Button inputs
        inputs.Player.Interact.performed += OnInteractInput;
        inputs.Player.Interact.canceled += OnInteractInput;
        inputs.Player.Jump.performed += OnJumpInput;
        inputs.Player.Jump.canceled += OnJumpInput;
        inputs.Player.Crouch.performed += OnCrouchInput;
        inputs.Player.Crouch.canceled += OnCrouchInput;
        inputs.Player.Sprint.performed += OnRunInput;
        inputs.Player.Sprint.canceled += OnRunInput;
        
        Debug.Log("[InputSystem] Initialized");
    }

    void OnEnable()
    {
        inputs.Player.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        inputs.Player.Disable();
    }
    
    private void Update()
    {
        // Continuously send look input every frame (fixes controller issue)
        // This ensures camera updates even when stick is held in same position
        if (isLooking || currentLookInput.sqrMagnitude > 0.0001f)
        {
            EventBus.Raise(new onLookInputEvent()
            {
                pressed = isLooking,
                Delta = currentLookInput
            });
        }
        
        // Continuously send move input for smooth movement
        EventBus.Raise(new onMoveInputEvent()
        {
            pressed = isMoving,
            Direction = currentMoveInput
        });
    }
    
    // ========================================================================
    // MOVEMENT INPUT
    // ========================================================================
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector2>();
        isMoving = true;
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMoveInput = Vector2.zero;
        isMoving = false;
    }
    
    // ========================================================================
    // LOOK INPUT
    // ========================================================================
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        currentLookInput = context.ReadValue<Vector2>();
        isLooking = true;
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        currentLookInput = Vector2.zero;
        isLooking = false;
    }
    
    // ========================================================================
    // BUTTON INPUTS
    // ========================================================================

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new onInteractInputEvent()
        {
            pressed = context.performed
        });
    }

    private void OnRunInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new onRunInputEvent()
        {
            pressed = context.performed
        });
    }

    private void OnCrouchInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new onCrouchInputEvent()
        {
            pressed = context.performed
        });
    }

    private void OnJumpInput(InputAction.CallbackContext context)
    {
        EventBus.Raise(new onJumpInputEvent()
        {
            pressed = context.performed
        });
    }
}
