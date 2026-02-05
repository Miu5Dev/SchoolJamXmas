using System.Collections;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class PlayerController : MonoBehaviour
{
    /// <summary>
    /// MOVEMENT VARIABLES
    /// </summary>
    [Header("Obligatory")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform cameraTransform;
    
    [Header("Speed Values")]
    [SerializeField] private float minSpeed = 0.0f;
    [SerializeField] private float currentSpeed = 1.0f;
    [SerializeField] private float speedGain = 0.2f;
    [SerializeField] private float speedLose = 0.05f;
    [SerializeField] private float maxSpeed = 16f;
    [SerializeField] private float maxCrouchingSpeed = 8.0f;
    [SerializeField] private float AirDivider = 8f;
    [SerializeField] private float CrouchDivider = 8f;
    
    [Header("Jump Values")]
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private float noSlopeProjectionTime = 0.15f;
    
    [Header("On Slope Movement Rotation Settings")]
    [SerializeField] private float rotationSmoothSpeed = 8f;
    
    // ========================================================================
    // MARIO 64 STYLE ROTATION SETTINGS
    // ========================================================================
    [Header("Mario 64 Style Rotation")]
    [Tooltip("Speed threshold to consider player 'stopped' for instant rotation")]
    [SerializeField] private float stoppedSpeedThreshold = 0.5f;
    
    [Tooltip("Angle threshold to trigger skid/brake turn (usually 120-150)")]
    [SerializeField] private float skidAngleThreshold = 130f;
    
    [Tooltip("How fast player rotates when stopped (instant feel)")]
    [SerializeField] private float stoppedRotationSpeed = 2000f;
    
    [Tooltip("How fast player rotates during normal arc movement")]
    [SerializeField] private float arcRotationSpeed = 360f;
    
    [Tooltip("How fast the arc turn blends direction")]
    [SerializeField] private float arcTurnBlendSpeed = 6f;
    
    [Tooltip("How fast player decelerates during skid")]
    [SerializeField] private float skidDeceleration = 25f;
    
    [Tooltip("Speed at which skid ends and player can turn")]
    [SerializeField] private float skidEndSpeed = 2f;
    
    [Tooltip("How fast player rotates at the END of skid (quick snap)")]
    [SerializeField] private float skidEndRotationSpeed = 1500f;
    
    [Tooltip("Minimum time in skid before can exit")]
    [SerializeField] private float minSkidTime = 0.1f;
    
    [Header("Rotation State (for animations)")]
    [SerializeField] private float rotationStateSmoothing = 5f;

    [Header("Movement Style")]
    [SerializeField] private float directionChangePenalty = 0.5f;
    [SerializeField] private float directionChangeThreshold = 90f;
    
    [Header("Slide State")]
    [SerializeField] private bool isSliding = false;
    [SerializeField] private bool isBeingPushedDown = false;
    [SerializeField] private float slideControlMultiplier = 1f;
    [SerializeField] private Vector3 slideDirection = Vector3.zero;
    [SerializeField] private float slideSpeedGain = 0f;
    [SerializeField] private float slideMomentumDecay = 0f;
    [SerializeField] private float slideMaxSpeed = 15f;
    
    [Header("Gravity Values")]
    [SerializeField] private float Gravity = -9.81f;
    
    [Header("Inputs Handler")]
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool isAction = false;
    [SerializeField] private bool isSwap = false;
    [SerializeField] private bool isJumping = false;
    
    [Header("DEBUG")]
    [SerializeField] private Vector3 moveDirection = Vector3.zero;
    [SerializeField] private Vector3 targetMoveDirection = Vector3.zero;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    [SerializeField] private Vector3 SlopeNormal = Vector3.zero;
    [SerializeField] private float currentSlopeAngle = 0f;
    [SerializeField] public float verticalVelocity = 0f;
    [SerializeField] private bool grounded = false;
    [SerializeField] private bool DirectionChanged = false;
    [SerializeField] private bool isInHangTime = false;
    [SerializeField] private Vector3 inputMoveDirection = Vector3.zero;
    [SerializeField] private bool ableToMove = true;
    [SerializeField] private float currentRotationState = 1f;
    
    [Header("DEBUG - Mario 64 Turn State")]
    [SerializeField] private TurnState currentTurnState = TurnState.None;
    [SerializeField] private float skidStartTime = -1f;
    [SerializeField] private Vector3 skidTargetDirection = Vector3.zero;
    [SerializeField] private float currentTurnAngle = 0f;

    /// <summary>
    /// PRIVATE VARIABLES
    /// </summary>
    private bool RisingSpeed = false;
    private float lastJumpTime = -1f;
    private bool StopEventSended = false;
    
    // Turn state enum
    public enum TurnState
    {
        None,           // No turn in progress
        ArcTurn,        // Smooth curved turn
        Skidding,       // Braking/skidding before 180
        SkidTurning     // Quick turn at end of skid
    }

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
    }
    
    void OnEnable()
    {
        EventBus.Subscribe<OnMoveInputEvent>(MoveInputUpdater);
        EventBus.Subscribe<OnJumpInputEvent>(JumpToggle);
        EventBus.Subscribe<OnActionInputEvent>(ActionToggle);
        EventBus.Subscribe<OnCrouchInputEvent>(CrouchToggle);
        EventBus.Subscribe<OnSwapInputEvent>(SwapToggle);
        
        EventBus.Subscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand);
        EventBus.Subscribe<OnApplyJumpForceCommand>(OnApplyJumpForce);
        EventBus.Subscribe<OnSetHangTimeState>(OnSetHangTimeState);
        EventBus.Subscribe<OnRotatePlayerCommand>(OnRotatePlayer);
        EventBus.Subscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Subscribe<OnPlayerStopSlidingEvent>(OnStopSliding);

        EventBus.Subscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Subscribe<onDialogueClose>(open => ableToMove = true);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<OnMoveInputEvent>(MoveInputUpdater);
        EventBus.Unsubscribe<OnJumpInputEvent>(JumpToggle);
        EventBus.Unsubscribe<OnActionInputEvent>(ActionToggle);
        EventBus.Unsubscribe<OnCrouchInputEvent>(CrouchToggle);
        EventBus.Unsubscribe<OnSwapInputEvent>(SwapToggle);
        
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Unsubscribe<OnExecuteJumpCommand>(OnExecuteJumpCommand);
        EventBus.Unsubscribe<OnApplyJumpForceCommand>(OnApplyJumpForce);
        EventBus.Unsubscribe<OnSetHangTimeState>(OnSetHangTimeState);
        EventBus.Unsubscribe<OnRotatePlayerCommand>(OnRotatePlayer);
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Unsubscribe<OnPlayerStopSlidingEvent>(OnStopSliding);
        
        EventBus.Unsubscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Unsubscribe<onDialogueClose>(open => ableToMove = true);
    }
    
    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        isSliding = ev.IsSliding;
        isBeingPushedDown = ev.IsBeingPushedDown;
        slideControlMultiplier = ev.ControlMultiplier;
        slideDirection = ev.SlideDirection;
        slideSpeedGain = ev.TargetSpeedGain;
        slideMomentumDecay = ev.MomentumDecay;
        slideMaxSpeed = ev.MaxSlideSpeed;
    }

    private void OnStopSliding(OnPlayerStopSlidingEvent ev)
    {
        isSliding = false;
        isBeingPushedDown = false;
        slideControlMultiplier = 1f;
        slideDirection = Vector3.zero;
        slideSpeedGain = 0f;
        slideMomentumDecay = 0f;
        slideMaxSpeed = 15f;
    }

    private void OnRotatePlayer(OnRotatePlayerCommand cmd)
    {
        if (cmd.InvertMovementDirection)
        {
            moveDirection = -moveDirection;
            targetMoveDirection = -targetMoveDirection;
        }
    }
    
    private void OnSetHangTimeState(OnSetHangTimeState state)
    {
        isInHangTime = state.IsInHangTime;
    }
    
    private void OnApplyJumpForce(OnApplyJumpForceCommand cmd)
    {
        verticalVelocity = cmd.Force;
    }
    
    private void OnExecuteJumpCommand(OnExecuteJumpCommand cmd)
    {
        ExecuteJump(cmd.JumpType);
    }
    
    private void Update()
    {
        GravityHandler();
        
        if (!ableToMove) return;
        
        CalculateCameraRelativeMovement();
        HandleMario64Movement();
        SpeedController();
        MovementController();
    }

    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = true;
        
        // Reset skid state when landing
        if (currentTurnState == TurnState.Skidding || currentTurnState == TurnState.SkidTurning)
        {
            // Keep the skid going if we were skidding in air (rare but possible)
        }
    }

    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        grounded = false;
        
        // Cancel skid when going airborne
        if (currentTurnState == TurnState.Skidding || currentTurnState == TurnState.SkidTurning)
        {
            currentTurnState = TurnState.None;
            
            EventBus.Raise(new OnPlayerSkidEvent()
            {
                Player = gameObject,
                IsSkidding = false,
                SkidDirection = Vector3.zero
            });
        }
    }

    private void OnSlope(OnPlayerSlopeEvent ev)
    {
        SlopeNormal = ev.SlopeNormal;
        currentSlopeAngle = ev.SlopeAngle;
    }

    private void CalculateCameraRelativeMovement()
    {
        DirectionChanged = false;
        
        if (inputDirection.magnitude > 0.1f)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0;
            cameraRight.y = 0;
            cameraForward.Normalize();
            cameraRight.Normalize();
            
            inputMoveDirection = (cameraForward * inputDirection.y + cameraRight * inputDirection.x).normalized;
        }
        else
        {
            inputMoveDirection = Vector3.zero;
        }
    }

    // ========================================================================
    // MARIO 64 STYLE MOVEMENT SYSTEM
    // ========================================================================
    
    private void HandleMario64Movement()
    {
        // Skip Mario 64 movement logic when sliding
        if (isSliding)
        {
            HandleSlidingMovement();
            return;
        }
        
        // Skip when in air (use simpler air control)
        if (!grounded)
        {
            HandleAirMovement();
            return;
        }
        
        // No input - just decelerate, no turn logic
        if (inputMoveDirection.magnitude < 0.1f)
        {
            currentTurnState = TurnState.None;
            return;
        }
        
        // Calculate angle between current facing and input
        float angleToInput = 0f;
        if (moveDirection.magnitude > 0.1f)
        {
            angleToInput = Vector3.Angle(moveDirection, inputMoveDirection);
        }
        currentTurnAngle = angleToInput;
        
        // === STATE MACHINE FOR TURNING ===
        
        switch (currentTurnState)
        {
            case TurnState.None:
                HandleNoTurnState(angleToInput);
                break;
                
            case TurnState.ArcTurn:
                HandleArcTurnState(angleToInput);
                break;
                
            case TurnState.Skidding:
                HandleSkiddingState();
                break;
                
            case TurnState.SkidTurning:
                HandleSkidTurningState();
                break;
        }
    }
    
    private void HandleNoTurnState(float angleToInput)
    {
        // CASE 1: Player is stopped or very slow - INSTANT rotation
        if (currentSpeed <= stoppedSpeedThreshold)
        {
            // Instant direction change
            moveDirection = inputMoveDirection;
            targetMoveDirection = inputMoveDirection;
            
            // Instant rotation
            if (inputMoveDirection.magnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(inputMoveDirection);
            }
            return;
        }
        
        // CASE 2: Moving and big angle change - Start SKID
        if (angleToInput >= skidAngleThreshold)
        {
            StartSkid();
            return;
        }
        
        // CASE 3: Moving and moderate angle - Start ARC TURN
        if (angleToInput > 10f)
        {
            currentTurnState = TurnState.ArcTurn;
            HandleArcTurnState(angleToInput);
            return;
        }
        
        // CASE 4: Moving straight or very small angle - direct movement
        moveDirection = inputMoveDirection;
        targetMoveDirection = inputMoveDirection;
        RotateTowardsDirection(inputMoveDirection, arcRotationSpeed);
    }
    
    private void HandleArcTurnState(float angleToInput)
    {
        // Check if angle became too sharp - switch to skid
        if (angleToInput >= skidAngleThreshold && currentSpeed > skidEndSpeed)
        {
            StartSkid();
            return;
        }
        
        // Check if turn is complete
        if (angleToInput < 5f)
        {
            currentTurnState = TurnState.None;
            moveDirection = inputMoveDirection;
            targetMoveDirection = inputMoveDirection;
            return;
        }
        
        // Smooth arc turn - blend direction over time
        targetMoveDirection = inputMoveDirection;
        moveDirection = Vector3.Lerp(moveDirection, inputMoveDirection, arcTurnBlendSpeed * Time.deltaTime);
        moveDirection.Normalize();
        
        // Rotate player smoothly
        RotateTowardsDirection(moveDirection, arcRotationSpeed);
        
        // Apply some speed penalty based on turn sharpness
        float turnPenalty = Mathf.InverseLerp(0f, skidAngleThreshold, angleToInput);
        float penaltyMultiplier = Mathf.Lerp(1f, 0.95f, turnPenalty);
        // Speed penalty is handled in SpeedController
    }
    
    private void StartSkid()
    {
        currentTurnState = TurnState.Skidding;
        skidStartTime = Time.time;
        skidTargetDirection = inputMoveDirection;
        
        // Emit skid event for animations
        EventBus.Raise(new OnPlayerSkidEvent()
        {
            Player = gameObject,
            IsSkidding = true,
            SkidDirection = moveDirection // Direction we're skidding FROM
        });
        
        // Also emit direction change event
        EventBus.Raise(new OnDirectionChangeEvent()
        {
            Player = gameObject,
            AngleChange = currentTurnAngle,
            OldDirection = moveDirection,
            NewDirection = skidTargetDirection,
            PenaltyFactor = 1f
        });
    }
    
    private void HandleSkiddingState()
    {
        // During skid: 
        // - Player keeps moving in ORIGINAL direction (not rotating yet)
        // - Speed decreases rapidly
        // - Once speed is low enough, transition to SkidTurning
        
        // Keep the original movement direction (sliding/skidding)
        // Don't change moveDirection yet!
        
        // Update target in case player changes input
        if (inputMoveDirection.magnitude > 0.1f)
        {
            skidTargetDirection = inputMoveDirection;
        }
        
        // Decelerate
        currentSpeed -= skidDeceleration * Time.deltaTime;
        currentSpeed = Mathf.Max(currentSpeed, 0f);
        
        // Check if we can exit skid
        bool minTimePassed = Time.time > skidStartTime + minSkidTime;
        bool slowEnough = currentSpeed <= skidEndSpeed;
        
        if (minTimePassed && slowEnough)
        {
            // Transition to quick turn
            currentTurnState = TurnState.SkidTurning;
        }
        
        // Player faces original direction during skid (or slightly backwards for visual effect)
        // Keep current rotation - don't rotate during skid
    }
    
    private void HandleSkidTurningState()
    {
        // Quick snap rotation to new direction
        if (skidTargetDirection.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(skidTargetDirection);
            float step = skidEndRotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, step);
            
            // Check if rotation is complete
            float remainingAngle = Quaternion.Angle(transform.rotation, targetRot);
            
            if (remainingAngle < 5f)
            {
                // Snap to final rotation
                transform.rotation = targetRot;
                
                // Update movement direction
                moveDirection = skidTargetDirection;
                targetMoveDirection = skidTargetDirection;
                
                // Exit skid state
                currentTurnState = TurnState.None;
                
                // Emit end of skid
                EventBus.Raise(new OnPlayerSkidEvent()
                {
                    Player = gameObject,
                    IsSkidding = false,
                    SkidDirection = Vector3.zero
                });
            }
        }
        else
        {
            // No input during skid turn - just end it
            currentTurnState = TurnState.None;
            
            EventBus.Raise(new OnPlayerSkidEvent()
            {
                Player = gameObject,
                IsSkidding = false,
                SkidDirection = Vector3.zero
            });
        }
    }
    
    private void HandleAirMovement()
    {
        // In air: similar to arc turn but with less control
        if (inputMoveDirection.magnitude > 0.1f)
        {
            targetMoveDirection = inputMoveDirection;
            
            // Slower direction change in air
            float airBlendSpeed = arcTurnBlendSpeed * 0.5f;
            moveDirection = Vector3.Lerp(moveDirection, inputMoveDirection, airBlendSpeed * Time.deltaTime);
            moveDirection.Normalize();
            
            // Rotate towards movement direction
            RotateTowardsDirection(moveDirection, arcRotationSpeed * 0.7f);
        }
    }
    
    private void HandleSlidingMovement()
    {
        // Existing sliding logic
        Vector3 horizontalSlideDir = new Vector3(slideDirection.x, 0f, slideDirection.z).normalized;
        
        if (isBeingPushedDown)
        {
            if (inputDirection.magnitude > 0.1f)
            {
                float dotAgainstSlide = Vector3.Dot(inputMoveDirection, -horizontalSlideDir);
                
                if (dotAgainstSlide > 0)
                {
                    Vector3 perpendicularInput = inputMoveDirection - (dotAgainstSlide * -horizontalSlideDir);
                    
                    if (perpendicularInput.magnitude > 0.1f)
                    {
                        perpendicularInput.Normalize();
                        moveDirection = Vector3.Lerp(horizontalSlideDir, perpendicularInput, slideControlMultiplier).normalized;
                    }
                    else
                    {
                        moveDirection = horizontalSlideDir;
                    }
                }
                else
                {
                    moveDirection = Vector3.Lerp(horizontalSlideDir, inputMoveDirection, slideControlMultiplier).normalized;
                }
            }
            else
            {
                moveDirection = horizontalSlideDir;
            }
        }
        else
        {
            if (inputDirection.magnitude > 0.1f)
            {
                moveDirection = Vector3.Lerp(moveDirection, inputMoveDirection, arcTurnBlendSpeed * Time.deltaTime).normalized;
            }
        }
        
        moveDirection.y = 0f;
        if (moveDirection.magnitude > 0.1f)
        {
            moveDirection.Normalize();
        }
        
        targetMoveDirection = moveDirection;
        
        // Rotate during sliding
        if (moveDirection.magnitude > 0.1f)
        {
            RotateTowardsDirection(moveDirection, arcRotationSpeed);
        }
    }
    
    private void RotateTowardsDirection(Vector3 direction, float rotationSpeed)
    {
        if (direction.magnitude < 0.1f) return;
        
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        float step = rotationSpeed * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, step);
    }

    // ========================================================================
    // SPEED CONTROLLER
    // ========================================================================

    private void SpeedController()
    {
        float moveDirectionMultiplier = Mathf.Clamp(inputDirection.magnitude, 0.1f, 1f);
        
        // Sliding speed is handled separately
        if (isSliding)
        {
            if (isBeingPushedDown)
            {
                currentSpeed += slideSpeedGain * Time.deltaTime;
                currentSpeed = Mathf.Min(currentSpeed, slideMaxSpeed);
            }
            else
            {
                currentSpeed -= slideMomentumDecay * Time.deltaTime;
                currentSpeed = Mathf.Max(currentSpeed, minSpeed);
            }
            return;
        }
        
        // Skidding speed is handled in HandleSkiddingState
        if (currentTurnState == TurnState.Skidding)
        {
            return; // Speed is controlled in skid handler
        }
        
        // Normal speed control
        if (grounded)
        {
            float maxSpd = this.maxSpeed;
            float spdLose = this.speedLose;
            
            if (isCrouching)
            {
                maxSpd = maxCrouchingSpeed;
                spdLose = this.speedLose / CrouchDivider;
            }

            if (RisingSpeed)
            {
                if (currentSpeed > maxSpd)
                {
                    currentSpeed -= spdLose;
                }
                else if (currentSpeed < maxSpd)
                {
                    // Apply turn penalty to acceleration
                    float turnPenalty = 1f;
                    if (currentTurnState == TurnState.ArcTurn)
                    {
                        turnPenalty = Mathf.Lerp(1f, 0.5f, currentTurnAngle / skidAngleThreshold);
                    }
                    
                    currentSpeed += speedGain * moveDirectionMultiplier * turnPenalty;
                    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpd);
                }
            }
            else
            {
                if (currentSpeed > minSpeed)
                {
                    currentSpeed -= spdLose;
                    currentSpeed = Mathf.Max(currentSpeed, minSpeed);
                }
            }
        }
        else
        {
            // Air speed control
            if (RisingSpeed)
            {
                if (currentSpeed > maxSpeed)
                {
                    currentSpeed -= speedLose;
                }
                else if (currentSpeed < maxSpeed)
                {
                    currentSpeed += (speedGain * moveDirectionMultiplier) / AirDivider;
                    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
                }
            }
            else
            {
                if (currentSpeed > minSpeed)
                {
                    currentSpeed -= speedLose;
                    currentSpeed = Mathf.Max(currentSpeed, minSpeed);
                }
            }
        }
    }

    // ========================================================================
    // MOVEMENT CONTROLLER
    // ========================================================================

    private void MovementController()
    {
        if (moveDirection.magnitude > 0.1)
        {
            // Calculate rotation state for animations
            float targetRotationState = 1f;

            if (inputMoveDirection.magnitude > 0.1f)
            {
                Vector3 localInputDir = transform.InverseTransformDirection(inputMoveDirection);
                targetRotationState = 1f + Mathf.Clamp(localInputDir.x, -1f, 1f);
            }

            currentRotationState = Mathf.Lerp(currentRotationState, targetRotationState, rotationStateSmoothing * Time.deltaTime);

            EventBus.Raise<OnPlayerMoveEvent>(new OnPlayerMoveEvent()
            {
                Player = this.gameObject,
                Direction = new Vector2(moveDirection.x, moveDirection.z),
                Rotation = Quaternion.LookRotation(moveDirection),
                speed = currentSpeed,
                isCrouching = isCrouching && grounded,
                rotationState = currentRotationState
            });
        }

        // Handle stop event
        if (inputDirection.magnitude > 0.1 && currentSpeed > minSpeed)
        {
            StopEventSended = false;
        }
        
        if (inputDirection.magnitude < 0.1 && currentSpeed <= minSpeed && !StopEventSended)
        {
            Debug.Log("Stopped");
            EventBus.Raise<OnPlayerStopEvent>(new OnPlayerStopEvent()
            {
                Player = this.gameObject
            });
            StopEventSended = true;
        }

        // Calculate final movement
        Vector3 finalMoveDirection = moveDirection;

        bool recentlyJumped = Time.time < lastJumpTime + noSlopeProjectionTime;

        // Project onto slope when grounded
        if (SlopeNormal != Vector3.zero && SlopeNormal != Vector3.up && grounded && !recentlyJumped)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                finalMoveDirection = Vector3.ProjectOnPlane(moveDirection, SlopeNormal);
            }
        }

        controller.Move(finalMoveDirection * (currentSpeed * Time.deltaTime));
    }

    // ========================================================================
    // INPUT HANDLERS
    // ========================================================================

    private void JumpToggle(OnJumpInputEvent e)
    {
        isJumping = e.pressed;
    }
    
    private void ActionToggle(OnActionInputEvent e)
    {
        isAction = e.pressed;
    }

    private void SwapToggle(OnSwapInputEvent e)
    {
        isSwap = e.pressed;
    }
    
    private void CrouchToggle(OnCrouchInputEvent e)
    {
        isCrouching = e.pressed;
    }
    
    private void MoveInputUpdater(OnMoveInputEvent e)
    {
        if (e.Direction.magnitude > 0.1f)
        {
            inputDirection = e.Direction;
            RisingSpeed = true;
        }
        else
        {
            inputDirection = Vector2.zero;
            RisingSpeed = false;
        }
    }
    
    // ========================================================================
    // JUMP EXECUTION
    // ========================================================================

    private void ExecuteJump(JumpTypeCreator jumpType)
    {
        // Cancel any skid when jumping
        if (currentTurnState == TurnState.Skidding || currentTurnState == TurnState.SkidTurning)
        {
            currentTurnState = TurnState.None;
            
            EventBus.Raise(new OnPlayerSkidEvent()
            {
                Player = gameObject,
                IsSkidding = false,
                SkidDirection = Vector3.zero
            });
        }
        
        float jumpSpeed;
        
        if (jumpType.resetSpeedOnJump)
        {
            currentSpeed = 0f;
        }
        
        if (jumpType.hangTime > 0f)
        {
            jumpSpeed = 0f;
        }
        else if (jumpType.jumpForce <= 0)
        {
            jumpSpeed = jumpType.jumpForce * 10f;
        }
        else
        {
            jumpSpeed = Mathf.Sqrt(jumpType.jumpForce * -2f * Gravity);
        }
        
        if (SlopeNormal != Vector3.zero && SlopeNormal != Vector3.up && !jumpType.ignoreUphillPenalty)
        {
            Vector3 slopeUpDirection = Vector3.ProjectOnPlane(Vector3.up, SlopeNormal).normalized;
            float upwardMovement = Vector3.Dot(moveDirection, slopeUpDirection);
            bool isGoingUphill = upwardMovement > 0.5f;
            
            verticalVelocity = jumpSpeed;
            
            if (isGoingUphill)
            {
                currentSpeed = Mathf.Min(currentSpeed * jumpType.uphillSpeedMultiplier, maxSpeed);
            }
            else
            {
                currentSpeed += jumpType.extraSpeed;
            }
        }
        else
        {
            verticalVelocity = jumpSpeed;
            currentSpeed += jumpType.extraSpeed;
        }
        
        lastJumpTime = Time.time;
        grounded = false;
    }

    // ========================================================================
    // GRAVITY
    // ========================================================================

    private void GravityHandler()
    {
        bool canLand = Time.time > lastJumpTime + jumpCooldown;
        
        if (grounded && verticalVelocity < 0 && canLand)
        {
            verticalVelocity = Mathf.Clamp(verticalVelocity, -9.81f, 1000);
        }
        
        if (Time.time > lastJumpTime + 0.05f && !isInHangTime)
        {
            verticalVelocity += Gravity * Time.deltaTime;
        }
        
        Vector3 verticalMovement = new Vector3(0, verticalVelocity, 0);
        controller.Move(verticalMovement * Time.deltaTime);
    }
    
    // ========================================================================
    // PUBLIC GETTERS
    // ========================================================================
    
    public TurnState GetCurrentTurnState() => currentTurnState;
    public bool IsSkidding() => currentTurnState == TurnState.Skidding || currentTurnState == TurnState.SkidTurning;
    public float GetCurrentSpeed() => currentSpeed;
}