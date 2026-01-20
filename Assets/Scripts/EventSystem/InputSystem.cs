using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{

    private MyInputs inputs;


    private void Awake()
    {
        inputs = new MyInputs();

        inputs.Player.Move.performed += onMoveInput;
        inputs.Player.Move.canceled += onMoveInput;
        inputs.Player.Interact.performed += onInteractInput;
        inputs.Player.Interact.canceled += onInteractInput;
        inputs.Player.Jump.performed += onJumpInput;
        inputs.Player.Jump.canceled += onJumpInput;
        inputs.Player.Crouch.performed += onCrouchInput;
        inputs.Player.Crouch.canceled += onCrouchInput;
        inputs.Player.Sprint.performed += onRunInput;
        inputs.Player.Sprint.canceled += onRunInput;
        
        Debug.Log("Input System Awake");
    }

    void OnEnable()
    {
        inputs.Player.Enable();
    }

    void OnDisable()
    {
        inputs.Player.Disable();
    }
    
    private void onMoveInput(InputAction.CallbackContext context)
    {
        
        bool refpress = context.performed;
        
        EventBus.Raise(new onMoveInputEvent()
        {
            pressed = refpress,
            Direction = context.ReadValue<Vector2>()
        });
    }

    private void onInteractInput(InputAction.CallbackContext context)
    {
        
        bool refpress = context.performed;
        
        
        EventBus.Raise(new onInteractInputEvent()
        {
            pressed = refpress
        });
    }

    private void onRunInput(InputAction.CallbackContext context)
    {
        bool refpress = context.performed;

        
        EventBus.Raise(new onRunInputEvent()
        {
            pressed = refpress
        });
    }

    private void onCrouchInput(InputAction.CallbackContext context)
    {
        bool refpress = context.performed;
        
        EventBus.Raise(new onCrouchInputEvent()
        {
            pressed = refpress
        });
    }

    
    private void onJumpInput(InputAction.CallbackContext context)
    {
        bool refpress = context.performed;

        EventBus.Raise(new onJumpInputEvent()
        {
            pressed = refpress
        });
    }
}
