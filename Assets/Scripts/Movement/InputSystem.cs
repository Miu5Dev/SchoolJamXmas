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

    private void Awake()
    {
        inputs = new MyInputs();

        // Movement
        inputs.Player.Move.performed += OnMovePerformed;
        inputs.Player.Move.canceled += OnMoveCanceled;
        
        // Look
        inputs.Player.Look.performed += OnLookPerformed;
        inputs.Player.Look.canceled += OnLookCanceled;
        
        // Button inputs
        inputs.Player.Interact.performed += OnInteractInput;
        inputs.Player.Interact.canceled += OnInteractInput;
        inputs.Player.Jump.performed += OnJumpInput;
        inputs.Player.Jump.canceled += OnJumpInput;
        inputs.Player.Crouch.performed += OnCrouchInput;
        inputs.Player.Crouch.canceled += OnCrouchInput;
        
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
        // Send move input every frame
        EventBus.Raise(new onMoveInputEvent()
        {
            pressed = currentMoveInput.sqrMagnitude > 0.01f,
            Direction = currentMoveInput
        });
        
        // Send look input every frame (for controller support)
        EventBus.Raise(new onLookInputEvent()
        {
            pressed = currentLookInput.sqrMagnitude > 0.01f,
            Delta = currentLookInput
        });
    }
    
    // ========================================================================
    // MOVEMENT INPUT
    // ========================================================================
    
    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        currentMoveInput = context.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        currentMoveInput = Vector2.zero;
    }
    
    // ========================================================================
    // LOOK INPUT
    // ========================================================================
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        currentLookInput = context.ReadValue<Vector2>();
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        // IMPORTANT: Reset to zero when released
        currentLookInput = Vector2.zero;
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
