using UnityEngine;

/// <summary>
/// Main player controller that coordinates all movement subsystems.
/// This is the central hub that manages state and delegates to specialized components.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerGroundDetection))]
[RequireComponent(typeof(MomentumSystem))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Movement Settings")]
    [SerializeField] private float minSpeed = 2f;               // Starting speed
    [SerializeField] private float maxSpeed = 12f;              // Maximum speed
    [SerializeField] private float maxCombinedSpeed = 25f;      // Max combined speed (momentum + speed) to prevent glitches
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float accelerationTime = 2f;       // Time to reach max speed
    [SerializeField] private float deceleration = 80f;          // Increased for better ground friction
    [SerializeField] private float turnSpeed = 720f;
    
    [Header("Air Control")]
    [SerializeField, Range(0f, 1f)] 
    private float airControlFraction = 0.125f;                  // 1/8 of ground control
    [SerializeField] private float airAcceleration = 15f;
    [SerializeField] private float airMomentumDecay = 8f;
    [SerializeField] private float airMomentumDecayControlled = 0.2f;    // Multiplier when player has input
    [SerializeField] private float airMomentumDecayUncontrolled = 0.7f;  // Multiplier when no input
    
    [Header("Gravity")]
    [SerializeField] private float gravity = -25f;              // Reduced for floatier platformer feel
    [SerializeField] private float maxFallSpeed = -50f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;
    [SerializeField] private float tripleJumpForce = 15f;
    [SerializeField] private float longJumpForce = 8f;
    [SerializeField] private float longJumpHorizontalBoost = 15f;
    [SerializeField] private float backflipForce = 14f;
    [SerializeField] private float backflipHorizontalForce = 3f;
    [SerializeField] private float backflipMinSpeedForQuickTurn = 3f;
    [SerializeField] private float groundPoundJumpForce = 18f;
    [SerializeField] private float slopeJumpMultiplier = 1.2f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float tripleJumpWindow = 1.0f;     // INCREASED: More time for multi-jumps
    [SerializeField] private float minSpeedForMultiJump = 2f;   // Low requirement
    [SerializeField] private float jumpSpeedBonus = 1.5f;       // Each jump adds speed
    [SerializeField] private float multiJumpDirectionTolerance = 0.7f;  // Dot product threshold for multi-jump direction (0.7 = ~45 degrees)
    
    [Header("Crouch Settings")]
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float colliderCenterY = 0f;        // FIXED: Configurable center Y
    
    [Header("Crouch Slide Settings")]
    [SerializeField] private float crouchSlideBoost = 8f;
    [SerializeField] private float crouchSlideMinSpeed = 4f;
    [SerializeField] private float crouchSlideFriction = 5f;
    [SerializeField] private float crouchSlideBackwardsFrictionMultiplier = 2.5f;
    [SerializeField] private float crouchSlideMaxDuration = 1.5f;
    
    [Header("Ground Pound Settings")]
    [SerializeField] private float groundPoundDelay = 0.2f;
    [SerializeField] private float groundPoundSpeed = 30f;
    [SerializeField] private float groundPoundJumpWindow = 0.3f;
    
    [Header("Dive Settings")]
    [SerializeField] private float diveForce = 10f;
    [SerializeField] private float diveHorizontalSpeed = 15f;
    [SerializeField] private float diveDuration = 0.5f;
    
    [Header("Slope Slide Settings")]
    [SerializeField] private float slideAccelerationMin = 3f;
    [SerializeField] private float slideAccelerationMax = 12f;
    
    [Header("Ledge Grab Settings")]
    [SerializeField] private float ledgeGrabDistance = 0.6f;
    [SerializeField] private float ledgeGrabHeight = 0.5f;
    [SerializeField] private float ledgeClimbDuration = 0.3f;
    [SerializeField] private float ledgeJumpForce = 10f;
    [SerializeField] private float ledgeMoveSpeed = 3f;
    [SerializeField] private LayerMask ledgeLayer = ~0;
    
    [Header("Edge Handling Settings")]
    [SerializeField] private float edgeUnstickForce = 25f;
    [SerializeField] private float edgeStuckVelocityThreshold = -5f;
    [SerializeField] private float edgeResponseTime = 0.05f;
    [SerializeField] private float edgeUnstickVelocity = -15f;
    
    [Header("Visual Settings")]
    [SerializeField] private bool alignToSlope = true;
    [SerializeField] private float slopeAlignmentSpeed = 10f;
    
    [Header("Debug Gizmos")]
    [SerializeField] private bool showMovementGizmos = true;
    [SerializeField] private bool showJumpGizmos = true;
    [SerializeField] private bool showStateGizmos = true;
    [SerializeField] private bool showColliderGizmos = true;
    [SerializeField] private bool showLedgeGizmos = true;
    
    [Header("Current State (Debug)")]
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    [SerializeField] private Vector2 inputDirection;
    [SerializeField] private float currentSpeed;
    [SerializeField] private float targetSpeed;
    [SerializeField] private float moveTime;                    // Time spent moving (for acceleration)
    [SerializeField] private float verticalVelocity;
    [SerializeField] private int jumpCount;
    [SerializeField] private float timeSinceLastJump;
    [SerializeField] private bool isJumpHeld;
    [SerializeField] private bool isCrouchHeld;
    
    // Constants for momentum calculations
    private const float MIN_SYSTEM_MOMENTUM_THRESHOLD = 0.5f;
    private const float MOMENTUM_BLEND_FACTOR = 0.5f;
    private const float SYSTEM_MOMENTUM_BLEND = 0.4f;
    private const float MOMENTUM_SPEED_CAP_MULTIPLIER = 1.5f;
    private const float CROUCH_SLIDE_MOMENTUM_TRANSFER = 0.6f;
    private const float CROUCH_SLIDE_SPEED_RETENTION = 0.7f;
    private const float LANDING_MOMENTUM_BASE = 0.5f;
    private const float LANDING_MOMENTUM_ALIGNED = 0.8f;
    private const float LANDING_ALIGNMENT_THRESHOLD = 0.7f;
    private const float GROUND_TO_AIR_GRAVITY_REDUCTION = 0.5f;
    private const float GROUND_TO_AIR_GRACE_PERIOD = 0.1f;
    
    // Constants for edge handling
    private const float EDGE_STUCK_MOMENTUM_DECAY = 0.98f;
    private const float EDGE_MOMENTUM_PUSH_MULTIPLIER = 0.8f;
    private const float EDGE_INPUT_PUSH_MULTIPLIER = 0.5f;
    private const float MIN_HORIZONTAL_VELOCITY_THRESHOLD = 0.5f;
    private const float MIN_INPUT_DIRECTION_THRESHOLD = 0.1f;
    
    // Components
    private CharacterController controller;
    private PlayerGroundDetection groundDetection;
    private MomentumSystem momentumSystem;
    
    // Input state
    private bool wasJumpPressed;
    private bool wasCrouchPressed;
    
    // Timers
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float lastJumpTime;
    private float groundPoundTimer;
    private float groundPoundLandTime;
    private float crouchSlideTimer;
    private float diveTimer;
    private float ledgeClimbTimer;
    private float airTime;
    private float edgeStuckTimer;
    
    // State flags
    private bool isJumping;
    private bool isCrouching;
    private bool isCrouchSliding;
    private bool isGroundPounding;
    private bool isGroundPoundStarting;
    private bool canGroundPoundJump;
    private bool isDiving;
    private bool isGrabbingLedge;
    private bool isClimbingLedge;
    private bool crouchLocked;
    
    // Movement data
    private Vector3 moveDirection;
    private Vector3 jumpMomentum;
    private Vector3 lastJumpDirection;
    private Vector3 crouchSlideDirection;
    private Vector3 ledgePosition;
    private Vector3 ledgeNormal;
    private Vector3 ledgeClimbStartPos;
    private Vector3 ledgeClimbTarget;
    private JumpType lastJumpType;
    private float currentHeight;
    private Vector3 previousMoveDirection;
    private float quickTurnTimer;
    
    // Properties
    public PlayerState CurrentState => currentState;
    public bool IsGrounded => groundDetection.IsGrounded;
    public bool IsJumping => isJumping;
    public bool IsCrouching => isCrouching;
    public float CurrentSpeed => currentSpeed;
    public Vector3 Velocity => controller.velocity;
    
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        groundDetection = GetComponent<PlayerGroundDetection>();
        momentumSystem = GetComponent<MomentumSystem>();
        
        controller.slopeLimit = 90f; // We handle slopes manually
        currentHeight = normalHeight;
        
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    
    private void OnEnable()
    {
        EventBus.Subscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Subscribe<onCrouchInputEvent>(OnCrouchInput);
    }
    
    private void OnDisable()
    {
        EventBus.Unsubscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Unsubscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Unsubscribe<onCrouchInputEvent>(OnCrouchInput);
    }
    
    // ========================================================================
    // INPUT HANDLERS
    // ========================================================================
    
    private void OnMoveInput(onMoveInputEvent ev)
    {
        inputDirection = ev.Direction;
    }
    
    private void OnJumpInput(onJumpInputEvent ev)
    {
        isJumpHeld = ev.pressed;
    }
    
    private void OnCrouchInput(onCrouchInputEvent ev)
    {
        isCrouchHeld = ev.pressed;
    }
    
    // ========================================================================
    // UPDATE LOOP
    // ========================================================================
    
    private void Update()
    {
        // Update debug timer
        timeSinceLastJump = Time.time - lastJumpTime;
        
        // Handle special states first
        if (isClimbingLedge)
        {
            HandleLedgeClimb();
            return;
        }
        
        // Ground detection
        GroundCheckResult groundResult = groundDetection.CheckGround();
        
        // Check for ledge grab
        if (!groundResult.isGrounded && !isGrabbingLedge && !isClimbingLedge)
        {
            CheckLedgeGrab();
        }
        
        if (isGrabbingLedge)
        {
            HandleLedgeGrab();
            return;
        }
        
        // Update timers
        UpdateTimers(groundResult);
        
        // Handle landing
        if (groundResult.justLanded)
        {
            OnLand();
        }
        
        // Core update loop
        HandleCrouch();
        HandleMovement(groundResult);
        HandleJump(groundResult);
        HandleGroundPound();
        HandleDive();
        
        // Update momentum
        momentumSystem.UpdateMomentum(moveDirection, currentSpeed, groundResult.isGrounded);
        
        // Apply gravity
        ApplyGravity(groundResult);
        
        // Apply final movement
        ApplyFinalMovement(groundResult);
        
        // Update visual rotation
        UpdateVisualRotation(groundResult);
        
        // Update state
        UpdateState(groundResult);
        
        // Store previous input state
        wasJumpPressed = isJumpHeld;
        wasCrouchPressed = isCrouchHeld;
        previousMoveDirection = moveDirection;
    }
    
    // ========================================================================
    // TIMERS
    // ========================================================================
    
    private void UpdateTimers(GroundCheckResult groundResult)
    {
        // Coyote time
        if (groundResult.isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            airTime += Time.deltaTime;
        }
        
        // FIX #3A: Transfer full ground velocity to air momentum when leaving ground without jumping
        if (groundResult.justLeftGround && !isJumping)
        {
            // Transfer ALL ground momentum to air
            Vector3 groundVelocity = moveDirection * currentSpeed;
            Vector3 systemMomentum = momentumSystem.CurrentMomentum;
            
            // Take the combined momentum for a natural feel
            jumpMomentum = groundVelocity;
            if (systemMomentum.magnitude > MIN_SYSTEM_MOMENTUM_THRESHOLD)
            {
                jumpMomentum += systemMomentum * MOMENTUM_BLEND_FACTOR;
            }
            
            // Cap to prevent excessive speed
            if (jumpMomentum.magnitude > maxSpeed * MOMENTUM_SPEED_CAP_MULTIPLIER)
            {
                jumpMomentum = jumpMomentum.normalized * maxSpeed * MOMENTUM_SPEED_CAP_MULTIPLIER;
            }
        }
        
        // Jump buffer
        if (isJumpHeld && !wasJumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }
        
        // Ground pound jump window
        if (canGroundPoundJump && Time.time - groundPoundLandTime > groundPoundJumpWindow)
        {
            canGroundPoundJump = false;
            jumpCount = 0;
        }
        
        // Triple jump window - reset if too long since last jump
        if (jumpCount > 0 && groundResult.isGrounded && Time.time - lastJumpTime > tripleJumpWindow)
        {
            jumpCount = 0;
        }
        
        // Quick turn detection
        if (inputDirection.magnitude > 0.1f && previousMoveDirection.magnitude > 0.1f)
        {
            float dot = Vector3.Dot(moveDirection.normalized, previousMoveDirection.normalized);
            if (dot < -0.5f)
            {
                quickTurnTimer = 0.2f;
            }
        }
        quickTurnTimer -= Time.deltaTime;
        
        // Crouch slide timer
        if (isCrouchSliding)
        {
            crouchSlideTimer += Time.deltaTime;
        }
        
        // Dive timer
        if (isDiving)
        {
            diveTimer += Time.deltaTime;
            if (diveTimer >= diveDuration && groundResult.isGrounded)
            {
                EndDive();
            }
        }
    }
    
    // ========================================================================
    // CROUCH
    // ========================================================================
    
    private void HandleCrouch()
    {
        // Check if crouch locked
        if (crouchLocked && !isCrouchHeld && groundDetection.IsGrounded && !groundDetection.IsOnSteepSlope)
        {
            if (!CheckCeilingAbove())
            {
                crouchLocked = false;
            }
        }
        
        bool shouldCrouch = isCrouchHeld || crouchLocked;
        
        // Start crouch slide when pressing crouch while moving
        if (isCrouchHeld && !wasCrouchPressed && groundDetection.IsGrounded && !isCrouchSliding)
        {
            float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
            if (horizontalSpeed >= crouchSlideMinSpeed)
            {
                StartCrouchSlide();
            }
        }
        
        // Update crouch state
        if (shouldCrouch != isCrouching)
        {
            isCrouching = shouldCrouch;
            EventBus.Raise(new OnPlayerCrouchEvent
            {
                Player = gameObject,
                IsCrouching = isCrouching
            });
        }
        
        // Update collider height
        float targetHeight = isCrouching ? crouchHeight : normalHeight;
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        
        controller.height = currentHeight;
        // FIXED: Use configurable center Y instead of calculating it
        controller.center = new Vector3(0f, colliderCenterY, 0f);
    }
    
    private void StartCrouchSlide()
    {
        isCrouchSliding = true;
        crouchLocked = true;
        crouchSlideTimer = 0f;
        crouchSlideDirection = moveDirection.magnitude > 0.1f ? moveDirection.normalized : transform.forward;
        
        currentSpeed += crouchSlideBoost;
        
        EventBus.Raise(new OnPlayerCrouchSlideStartEvent
        {
            Player = gameObject,
            InitialSpeed = currentSpeed,
            SlideDirection = crouchSlideDirection
        });
    }
    
    private void EndCrouchSlide(CrouchSlideEndReason reason)
    {
        if (!isCrouchSliding) return;
        
        float finalSpeed = currentSpeed;
        isCrouchSliding = false;
        
        // FIX #4A: Transfer remaining speed to regular movement momentum
        if (finalSpeed > minSpeed && moveDirection.magnitude > 0.1f)
        {
            // Transfer slide momentum to movement system for smooth transition
            Vector3 slideMomentum = crouchSlideDirection * finalSpeed;
            momentumSystem.AddMomentum(slideMomentum * CROUCH_SLIDE_MOMENTUM_TRANSFER, MomentumSource.Movement);
            
            // Keep some speed in the current speed
            currentSpeed = Mathf.Max(currentSpeed, finalSpeed * CROUCH_SLIDE_SPEED_RETENTION);
        }
        
        EventBus.Raise(new OnPlayerCrouchSlideEndEvent
        {
            Player = gameObject,
            FinalSpeed = finalSpeed,
            Reason = reason
        });
    }
    
    private bool CheckCeilingAbove()
    {
        float checkDistance = normalHeight - crouchHeight + 0.1f;
        Vector3 checkPos = transform.position + Vector3.up * crouchHeight;
        return Physics.SphereCast(checkPos, controller.radius * 0.8f, Vector3.up, out _, checkDistance);
    }
    
    // ========================================================================
    // MOVEMENT - AUTO ACCELERATION (NO RUN BUTTON)
    // ========================================================================
    
    private void HandleMovement(GroundCheckResult groundResult)
    {
        // Calculate camera-relative input direction
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 inputDir = (cameraForward * inputDirection.y + cameraRight * inputDirection.x).normalized;
        
        // Handle crouch sliding
        if (isCrouchSliding)
        {
            HandleCrouchSlideMovement(inputDir, groundResult);
            return;
        }
        
        // Handle slope sliding
        if (groundResult.isOnSteepSlope)
        {
            HandleSlopeSlideMovement(inputDir, groundResult);
            return;
        }
        
        // Normal movement with AUTO ACCELERATION
        if (inputDir.magnitude > 0.1f)
        {
            // Increase move time while moving
            moveTime += Time.deltaTime;
            
            // Calculate target speed based on how long we've been moving
            if (isCrouching)
            {
                targetSpeed = crouchSpeed;
                moveTime = 0f; // Reset acceleration when crouching
            }
            else
            {
                // Lerp from minSpeed to maxSpeed based on move time
                float accelerationProgress = Mathf.Clamp01(moveTime / accelerationTime);
                targetSpeed = Mathf.Lerp(minSpeed, maxSpeed, accelerationProgress);
            }
            
            // Apply acceleration
            float accel = groundResult.isGrounded ? 
                (maxSpeed - minSpeed) / accelerationTime : // Smooth acceleration on ground
                airAcceleration * airControlFraction;       // Reduced in air
            
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accel * Time.deltaTime);
            
            // Rotate toward input direction
            if (groundResult.isGrounded && !groundDetection.IsOnSteepSlope)
            {
                Quaternion targetRotation = Quaternion.LookRotation(inputDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
            
            moveDirection = inputDir;
        }
        else
        {
            // Reset move time when not moving
            moveTime = 0f;
            
            // Decelerate
            float decel = groundResult.isGrounded ? deceleration : deceleration * 0.3f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);
        }
    }
    
    private void HandleCrouchSlideMovement(Vector3 inputDir, GroundCheckResult groundResult)
    {
        // Crouch slide should strongly reduce momentum until reaching crouch speed
        float baseFriction = crouchSlideFriction * 2f; // Increased friction
        
        // Check if player is trying to go backwards (faster decay)
        float backwardsMultiplier = 1f;
        if (inputDir.magnitude > 0.1f)
        {
            float alignment = Vector3.Dot(inputDir.normalized, crouchSlideDirection.normalized);
            if (alignment < -0.3f) // Moving opposite to slide direction
            {
                backwardsMultiplier = crouchSlideBackwardsFrictionMultiplier;
            }
        }
        
        // Apply friction with possible backwards multiplier
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, baseFriction * backwardsMultiplier * Time.deltaTime);
        
        // Don't turn the player during crouch slide
        // Allow slight lateral movement but maintain slide direction
        if (inputDir.magnitude > 0.1f)
        {
            // Only allow subtle lateral adjustments, not full turns
            Vector3 lateralDir = inputDir - Vector3.Project(inputDir, crouchSlideDirection);
            if (lateralDir.magnitude > 0.1f)
            {
                crouchSlideDirection = Vector3.Lerp(crouchSlideDirection, crouchSlideDirection + lateralDir * 0.15f, Time.deltaTime * 1.5f).normalized;
            }
        }
        
        moveDirection = crouchSlideDirection;
        
        // End crouch slide when speed drops below crouch walk speed
        if (currentSpeed < crouchSpeed)
        {
            EndCrouchSlide(CrouchSlideEndReason.SpeedTooLow);
            currentSpeed = crouchSpeed; // Transition to crouch walk
        }
        else if (crouchSlideTimer >= crouchSlideMaxDuration)
        {
            EndCrouchSlide(CrouchSlideEndReason.SpeedTooLow);
        }
        else if (!isCrouchHeld && !crouchLocked)
        {
            EndCrouchSlide(CrouchSlideEndReason.Released);
        }
        
        EventBus.Raise(new OnPlayerCrouchSlidingEvent
        {
            Player = gameObject,
            IsCrouchSliding = true,
            Speed = currentSpeed
        });
    }
    
    private void HandleSlopeSlideMovement(Vector3 inputDir, GroundCheckResult groundResult)
    {
        Vector3 slideDirection = groundDetection.GetSlideDirection();
        float angle = groundResult.angle;
        
        float angleNormalized = (angle - groundDetection.MaxWalkableAngle) / (90f - groundDetection.MaxWalkableAngle);
        float slideAccel = Mathf.Lerp(slideAccelerationMin, slideAccelerationMax, angleNormalized);
        
        currentSpeed += slideAccel * Time.deltaTime;
        
        // Allow lateral movement
        Vector3 lateralDir = inputDir - Vector3.Project(inputDir, slideDirection);
        if (lateralDir.magnitude > 0.1f)
        {
            lateralDir.Normalize();
            moveDirection = (slideDirection + lateralDir * 0.3f).normalized;
        }
        else
        {
            moveDirection = slideDirection;
        }
        
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, groundResult.normal);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * 0.5f * Time.deltaTime);
        }
        
        EventBus.Raise(new OnPlayerSlopeSlideEvent
        {
            Player = gameObject,
            IsSliding = true,
            SlideSpeed = currentSpeed,
            SlideDirection = slideDirection,
            SlopeAngle = angle
        });
    }
    
    // ========================================================================
    // JUMP
    // ========================================================================
    
    private void HandleJump(GroundCheckResult groundResult)
    {
        if (isGroundPounding || isGroundPoundStarting || isDiving) return;
        
        bool jumpPressed = isJumpHeld && !wasJumpPressed;
        bool canJump = (groundResult.isGrounded || coyoteTimer > 0f) && !isJumping;
        bool hasBufferedJump = jumpBufferTimer > 0f;
        
        // Jump cut removed - jumps always go to full height
        // (User feedback: Saltar tiene que ser siempre a altura maxima del salto)
        
        if ((jumpPressed || hasBufferedJump) && canJump)
        {
            jumpBufferTimer = 0f;
            DetermineAndPerformJump(groundResult);
        }
    }
    
    private void DetermineAndPerformJump(GroundCheckResult groundResult)
    {
        // Ground pound jump
        if (canGroundPoundJump)
        {
            PerformJump(JumpType.GroundPoundJump, groundPoundJumpForce, moveDirection * currentSpeed);
            canGroundPoundJump = false;
            return;
        }
        
        // Backflip on quick turn/direction change (not just when crouching)
        bool isStationary = currentSpeed < 1f;
        bool isPressingBack = inputDirection.y < -0.5f;
        bool didQuickTurn = quickTurnTimer > 0f;
        
        if ((isCrouching || isCrouchSliding) && (isStationary || isPressingBack || didQuickTurn))
        {
            // Backflip from crouch
            if (isCrouchSliding)
            {
                EndCrouchSlide(CrouchSlideEndReason.Jumped);
            }
            PerformJump(JumpType.Backflip, backflipForce, -transform.forward * backflipHorizontalForce);
            return;
        }
        else if (didQuickTurn && currentSpeed > backflipMinSpeedForQuickTurn)
        {
            // Backflip from quick direction change while moving
            PerformJump(JumpType.Backflip, backflipForce, -transform.forward * backflipHorizontalForce);
            return;
        }
        
        // Long jump from crouch slide
        if (isCrouchSliding && currentSpeed > crouchSlideMinSpeed)
        {
            EndCrouchSlide(CrouchSlideEndReason.Jumped);
            PerformJump(JumpType.Long, longJumpForce, transform.forward * longJumpHorizontalBoost);
            return;
        }
        
        // Slope jump
        if (groundResult.isOnSteepSlope)
        {
            Vector3 slopeDir = groundDetection.GetSlideDirection();
            PerformJump(JumpType.SlopeJump, jumpForce * slopeJumpMultiplier, slopeDir * currentSpeed);
            return;
        }
        
        // Multi-jump chain
        float horizontalSpeed = new Vector3(controller.velocity.x, 0, controller.velocity.z).magnitude;
        bool hasEnoughSpeed = horizontalSpeed >= minSpeedForMultiJump || currentSpeed >= minSpeedForMultiJump;
        bool inTimeWindow = Time.time - lastJumpTime < tripleJumpWindow;
        
        // Check if player is maintaining direction for multi-jump (allow ~45 degree variation)
        bool isSameDirection = true;
        if (lastJumpDirection.magnitude > 0.1f && moveDirection.magnitude > 0.1f)
        {
            float directionAlignment = Vector3.Dot(lastJumpDirection.normalized, moveDirection.normalized);
            isSameDirection = directionAlignment > multiJumpDirectionTolerance;
        }
        
        if (jumpCount == 1 && hasEnoughSpeed && inTimeWindow && isSameDirection)
        {
            PerformJump(JumpType.Double, doubleJumpForce, moveDirection * (currentSpeed + jumpSpeedBonus));
            return;
        }
        
        if (jumpCount == 2 && hasEnoughSpeed && inTimeWindow && isSameDirection)
        {
            PerformJump(JumpType.Triple, tripleJumpForce, moveDirection * (currentSpeed + jumpSpeedBonus * 2f));
            return;
        }
        
        // Normal jump
        PerformJump(JumpType.Normal, jumpForce, moveDirection * currentSpeed);
    }
    
    private void PerformJump(JumpType type, float force, Vector3 horizontalMomentum)
    {
        isJumping = true;
        lastJumpType = type;
        lastJumpTime = Time.time;
        coyoteTimer = 0f;
        airTime = 0f;
        
        verticalVelocity = force;
        
        // Track jump direction for multi-jump validation
        Vector3 horizontalDirection = new Vector3(horizontalMomentum.x, 0f, horizontalMomentum.z);
        if (horizontalDirection.magnitude > 0.1f)
        {
            lastJumpDirection = horizontalDirection.normalized;
        }
        
        // FIX #4B: Consider momentum system when calculating jump momentum for smoother bunny-hop chains
        Vector3 systemMomentum = momentumSystem.CurrentMomentum;
        if (systemMomentum.magnitude > MIN_SYSTEM_MOMENTUM_THRESHOLD)
        {
            // Blend horizontal momentum with system momentum for smoother chaining
            horizontalMomentum = Vector3.Lerp(horizontalMomentum, horizontalMomentum + systemMomentum * SYSTEM_MOMENTUM_BLEND, MOMENTUM_BLEND_FACTOR);
        }
        
        jumpMomentum = horizontalMomentum;
        momentumSystem.SetMomentum(horizontalMomentum, MomentumSource.Jump);
        
        // Update jump count
        if (type == JumpType.Normal) jumpCount = 1;
        else if (type == JumpType.Double) jumpCount = 2;
        else if (type == JumpType.Triple) jumpCount = 3;
        else jumpCount = 1;
        
        // Handle backflip
        if (type == JumpType.Backflip)
        {
            transform.rotation = Quaternion.Euler(0f, transform.eulerAngles.y + 180f, 0f);
            crouchLocked = false;
            isCrouching = false;
        }
        
        // Handle long jump
        if (type == JumpType.Long)
        {
            crouchLocked = false;
            isCrouching = false;
        }
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = type,
            JumpCount = jumpCount,
            JumpDirection = (Vector3.up + horizontalMomentum.normalized).normalized,
            JumpForce = force
        });
    }
    
    // ========================================================================
    // GROUND POUND
    // ========================================================================
    
    private void HandleGroundPound()
    {
        if (!groundDetection.IsGrounded && isCrouchHeld && !wasCrouchPressed && 
            !isGroundPounding && !isGroundPoundStarting && !isDiving)
        {
            if (verticalVelocity < jumpForce * 0.5f)
            {
                StartGroundPound();
            }
        }
        
        if (isGroundPoundStarting)
        {
            groundPoundTimer += Time.deltaTime;
            verticalVelocity = 0f;
            jumpMomentum = Vector3.zero;
            currentSpeed = 0f;
            
            if (groundPoundTimer >= groundPoundDelay)
            {
                isGroundPoundStarting = false;
                isGroundPounding = true;
                
                EventBus.Raise(new OnPlayerGroundPoundEvent
                {
                    Player = gameObject,
                    Phase = GroundPoundPhase.Falling
                });
            }
        }
    }
    
    private void StartGroundPound()
    {
        isGroundPoundStarting = true;
        isGroundPounding = false;
        groundPoundTimer = 0f;
        jumpMomentum = Vector3.zero;
        momentumSystem.ClearMomentum();
        
        EventBus.Raise(new OnPlayerGroundPoundEvent
        {
            Player = gameObject,
            Phase = GroundPoundPhase.Starting
        });
    }
    
    // ========================================================================
    // DIVE
    // ========================================================================
    
    private void HandleDive()
    {
        if (isGroundPoundStarting && isJumpHeld && !wasJumpPressed)
        {
            StartDive();
        }
    }
    
    private void StartDive()
    {
        isGroundPoundStarting = false;
        isGroundPounding = false;
        isDiving = true;
        diveTimer = 0f;
        
        Vector3 diveDir = transform.forward;
        
        verticalVelocity = diveForce;
        jumpMomentum = diveDir * diveHorizontalSpeed;
        momentumSystem.SetMomentum(jumpMomentum, MomentumSource.Jump);
        
        EventBus.Raise(new OnPlayerDiveEvent
        {
            Player = gameObject,
            DiveDirection = diveDir,
            DiveSpeed = diveHorizontalSpeed,
            FromGroundPound = true
        });
    }
    
    private void EndDive()
    {
        isDiving = false;
        isCrouching = true;
        crouchLocked = true;
    }
    
    // ========================================================================
    // LEDGE GRAB
    // ========================================================================
    
    private void CheckLedgeGrab()
    {
        if (groundDetection.IsGrounded || isGrabbingLedge || isClimbingLedge) return;
        if (verticalVelocity > 0f) return;
        if (isGroundPounding || isDiving) return;
        
        Vector3 forwardDir = transform.forward;
        Vector3 checkOrigin = transform.position + Vector3.up * (currentHeight - ledgeGrabHeight);
        
        if (Physics.Raycast(checkOrigin, forwardDir, out RaycastHit wallHit, ledgeGrabDistance, ledgeLayer))
        {
            Vector3 ledgeCheckOrigin = wallHit.point + forwardDir * 0.1f + Vector3.up * ledgeGrabHeight;
            
            if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit ledgeHit, ledgeGrabHeight + 0.2f, ledgeLayer))
            {
                Vector3 standCheckOrigin = ledgeHit.point + Vector3.up * (normalHeight * 0.5f + 0.1f);
                
                if (!Physics.CheckSphere(standCheckOrigin, controller.radius * 0.8f, ledgeLayer))
                {
                    GrabLedge(ledgeHit.point, wallHit.normal, wallHit.collider);
                }
            }
        }
    }
    
    private void GrabLedge(Vector3 ledgePoint, Vector3 wallNormal, Collider ledgeCollider)
    {
        isGrabbingLedge = true;
        ledgePosition = ledgePoint;
        ledgeNormal = wallNormal;
        verticalVelocity = 0f;
        jumpMomentum = Vector3.zero;
        momentumSystem.ClearMomentum();
        
        Vector3 hangPosition = ledgePoint + wallNormal * (controller.radius + 0.05f);
        hangPosition.y = ledgePoint.y - currentHeight + ledgeGrabHeight;
        transform.position = hangPosition;
        
        transform.rotation = Quaternion.LookRotation(-wallNormal);
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = true,
            LedgePosition = ledgePosition,
            LedgeNormal = ledgeNormal
        });
    }
    
    private void HandleLedgeGrab()
    {
        verticalVelocity = 0f;
        
        if (isJumpHeld && !wasJumpPressed)
        {
            LedgeJump();
            return;
        }
        
        if (inputDirection.y < -0.5f)
        {
            ReleaseLedge();
            return;
        }
        
        if (inputDirection.y > 0.5f)
        {
            StartLedgeClimb();
            return;
        }
        
        if (Mathf.Abs(inputDirection.x) > 0.1f)
        {
            MoveLedgeSideways(inputDirection.x);
        }
    }
    
    private void MoveLedgeSideways(float direction)
    {
        Vector3 right = Vector3.Cross(Vector3.up, -ledgeNormal).normalized;
        Vector3 moveDir = right * direction;
        
        Vector3 targetPos = transform.position + moveDir * ledgeMoveSpeed * Time.deltaTime;
        Vector3 checkOrigin = targetPos + Vector3.up * (currentHeight - ledgeGrabHeight);
        
        if (Physics.Raycast(checkOrigin, -ledgeNormal, out RaycastHit wallHit, ledgeGrabDistance, ledgeLayer))
        {
            Vector3 ledgeCheckOrigin = wallHit.point - ledgeNormal * 0.1f + Vector3.up * ledgeGrabHeight;
            
            if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit ledgeHit, ledgeGrabHeight + 0.2f, ledgeLayer))
            {
                // Smooth position update to prevent vibration
                Vector3 hangPosition = ledgeHit.point + wallHit.normal * (controller.radius + 0.05f);
                hangPosition.y = ledgeHit.point.y - currentHeight + ledgeGrabHeight;
                
                // Lerp position for smoother movement along ledge
                transform.position = Vector3.Lerp(transform.position, hangPosition, Time.deltaTime * 15f);
                
                ledgeNormal = wallHit.normal;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-ledgeNormal), Time.deltaTime * 12f);
                ledgePosition = ledgeHit.point;
                
                EventBus.Raise(new OnPlayerLedgeMoveEvent
                {
                    Player = gameObject,
                    MoveDirection = direction,
                    NewPosition = transform.position
                });
            }
        }
    }
    
    private void LedgeJump()
    {
        isGrabbingLedge = false;
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.LedgeJump;
        lastJumpTime = Time.time;
        
        verticalVelocity = ledgeJumpForce;
        jumpMomentum = ledgeNormal * ledgeJumpForce * 0.6f;
        momentumSystem.SetMomentum(jumpMomentum, MomentumSource.Jump);
        
        transform.rotation = Quaternion.LookRotation(ledgeNormal);
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = false,
            LedgePosition = ledgePosition,
            LedgeNormal = ledgeNormal
        });
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.LedgeJump,
            JumpCount = jumpCount,
            JumpDirection = ledgeNormal,
            JumpForce = ledgeJumpForce
        });
    }
    
    private void ReleaseLedge()
    {
        isGrabbingLedge = false;
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = false,
            LedgePosition = ledgePosition,
            LedgeNormal = ledgeNormal
        });
    }
    
    private void StartLedgeClimb()
    {
        isGrabbingLedge = false;
        isClimbingLedge = true;
        ledgeClimbTimer = 0f;
        ledgeClimbStartPos = transform.position;
        ledgeClimbTarget = ledgePosition - ledgeNormal * (controller.radius + 0.2f);
        ledgeClimbTarget.y = ledgePosition.y + 0.1f;
        
        EventBus.Raise(new OnPlayerLedgeClimbEvent
        {
            Player = gameObject,
            IsClimbing = true,
            Progress = 0f
        });
    }
    
    private void HandleLedgeClimb()
    {
        ledgeClimbTimer += Time.deltaTime;
        float t = ledgeClimbTimer / ledgeClimbDuration;
        
        EventBus.Raise(new OnPlayerLedgeClimbEvent
        {
            Player = gameObject,
            IsClimbing = true,
            Progress = t
        });
        
        if (t >= 1f)
        {
            transform.position = ledgeClimbTarget;
            isClimbingLedge = false;
            jumpCount = 0;
            
            EventBus.Raise(new OnPlayerLedgeClimbEvent
            {
                Player = gameObject,
                IsClimbing = false,
                Progress = 1f
            });
            return;
        }
        
        float upT = Mathf.Clamp01(t * 2f);
        float forwardT = Mathf.Clamp01((t - 0.3f) / 0.7f);
        
        Vector3 upPos = ledgeClimbStartPos + Vector3.up * (ledgeClimbTarget.y - ledgeClimbStartPos.y) * upT;
        Vector3 finalPos = Vector3.Lerp(upPos, ledgeClimbTarget, forwardT);
        
        transform.position = finalPos;
    }
    
    // ========================================================================
    // LANDING
    // ========================================================================
    
    private void OnLand()
    {
        float fallSpeed = Mathf.Abs(verticalVelocity);
        bool fromGroundPound = isGroundPounding;
        
        if (isGroundPounding)
        {
            isGroundPounding = false;
            isGroundPoundStarting = false;
            canGroundPoundJump = true;
            groundPoundLandTime = Time.time;
            
            EventBus.Raise(new OnPlayerGroundPoundEvent
            {
                Player = gameObject,
                Phase = GroundPoundPhase.Landing
            });
        }
        
        if (!canGroundPoundJump)
        {
            isJumping = false;
        }
        else
        {
            isJumping = false;
        }
        
        // FIX #4D: Speed preservation on landing - check alignment between input and momentum
        if (jumpMomentum.magnitude > 1f)
        {
            float momentumPreservation = LANDING_MOMENTUM_BASE;
            
            // If player is holding input in the same direction as momentum, preserve more speed
            if (inputDirection.magnitude > 0.1f && moveDirection.magnitude > 0.1f)
            {
                Vector3 horizontalMomentum = new Vector3(jumpMomentum.x, 0f, jumpMomentum.z);
                float alignment = Vector3.Dot(moveDirection.normalized, horizontalMomentum.normalized);
                
                // If aligned (moving same direction), preserve more momentum
                if (alignment > LANDING_ALIGNMENT_THRESHOLD)
                {
                    momentumPreservation = LANDING_MOMENTUM_ALIGNED; // Keep 80% of momentum
                }
            }
            
            momentumSystem.AddMomentum(jumpMomentum * momentumPreservation, MomentumSource.Movement);
        }
        jumpMomentum = Vector3.zero;
        airTime = 0f;
        
        EventBus.Raise(new OnPlayerLandEvent
        {
            Player = gameObject,
            FallSpeed = fallSpeed,
            HardLanding = fallSpeed > 20f,
            FromGroundPound = fromGroundPound
        });
    }
    
    // ========================================================================
    // GRAVITY & FINAL MOVEMENT
    // ========================================================================
    
    private void ApplyGravity(GroundCheckResult groundResult)
    {
        if (isGroundPounding)
        {
            verticalVelocity = -groundPoundSpeed;
            return;
        }
        
        if (isGroundPoundStarting)
        {
            verticalVelocity = 0f;
            return;
        }
        
        if (groundResult.isGrounded && !isJumping)
        {
            float stickForce = groundDetection.CalculateGroundStickForce(currentSpeed);
            verticalVelocity = -stickForce;
        }
        else
        {
            // FIX #3C & #4C: Reduce gravity briefly when just left ground for smoother transition
            float currentGravity = gravity;
            if (airTime < GROUND_TO_AIR_GRACE_PERIOD && !isJumping)
            {
                // Just left ground - reduce gravity briefly for smoother transition
                currentGravity *= GROUND_TO_AIR_GRAVITY_REDUCTION;
            }
            
            verticalVelocity += currentGravity * Time.deltaTime;
            verticalVelocity = Mathf.Max(verticalVelocity, maxFallSpeed);
        }
    }
    
    private void ApplyFinalMovement(GroundCheckResult groundResult)
    {
        Vector3 velocity;
        
        if (isGroundPounding || isGroundPoundStarting)
        {
            velocity = Vector3.up * verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
            return;
        }
        
        if (!groundResult.isGrounded)
        {
            // FIX #3B: Better air control and momentum preservation
            Vector3 airVelocity = jumpMomentum;
            
            // Calculate camera-relative input direction
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            Vector3 inputDir = (cameraForward * inputDirection.y + cameraRight * inputDirection.x).normalized;
            
            if (inputDir.magnitude > 0.1f)
            {
                inputDir.Normalize();
                
                // Stronger air control force (1.5x boost)
                Vector3 airControlForce = inputDir * airAcceleration * airControlFraction * 1.5f;
                airVelocity += airControlForce * Time.deltaTime;
                
                // Very slow decay when controlling - maintain momentum
                // Don't decay during coyote time (Fix #4E) for better feel
                if (coyoteTimer <= 0f)
                {
                    float decayRate = airMomentumDecay * airMomentumDecayControlled * Time.deltaTime;
                    airVelocity = Vector3.MoveTowards(airVelocity, Vector3.zero, decayRate);
                }
            }
            else
            {
                // Normal decay when not controlling, but still not too aggressive
                // Don't decay during coyote time (Fix #4E)
                if (coyoteTimer <= 0f)
                {
                    float decayRate = airMomentumDecay * airMomentumDecayUncontrolled * Time.deltaTime;
                    airVelocity = Vector3.MoveTowards(airVelocity, Vector3.zero, decayRate);
                }
            }
            
            jumpMomentum = airVelocity;
            velocity = airVelocity;
            velocity.y = verticalVelocity;
            
            EventBus.Raise(new OnPlayerAirborneEvent
            {
                Player = gameObject,
                AirTime = airTime,
                VerticalVelocity = verticalVelocity
            });
        }
        else
        {
            // Reset edge stuck timer when grounded
            edgeStuckTimer = 0f;
            
            Vector3 groundVelocity;
            
            if (groundResult.angle > 1f)
            {
                groundVelocity = groundDetection.ProjectOnSlope(moveDirection) * currentSpeed;
            }
            else
            {
                groundVelocity = moveDirection * currentSpeed;
            }
            
            velocity = momentumSystem.GetCombinedVelocity(groundVelocity);
            velocity.y = verticalVelocity;
        }
        
        // FIX #2: Edge detection and handling
        // Detect if stuck on edge: side collision + not grounded + falling
        // DISABLE when attempting ledge grab to avoid conflict
        CollisionFlags collisionFlags = controller.Move(velocity * Time.deltaTime);
        bool hasSideCollision = (collisionFlags & CollisionFlags.Sides) != 0;
        bool isAttemptingLedgeGrab = !groundResult.isGrounded && verticalVelocity < 0f && hasSideCollision && !isGrabbingLedge;
        bool isStuckOnEdge = hasSideCollision && !groundResult.isGrounded && verticalVelocity < edgeStuckVelocityThreshold && !isAttemptingLedgeGrab;
        
        if (isStuckOnEdge)
        {
            // Immediate strong push to unstick
            float pushStrength = edgeUnstickForce * Time.deltaTime;
            
            // Push down and slightly outward from the edge
            Vector3 unstickMove = Vector3.down * pushStrength;
            
            // Add momentum in movement direction to slide off naturally
            Vector3 horizontalVel = new Vector3(jumpMomentum.x, 0f, jumpMomentum.z);
            if (horizontalVel.magnitude > MIN_HORIZONTAL_VELOCITY_THRESHOLD)
            {
                unstickMove += horizontalVel.normalized * pushStrength * EDGE_MOMENTUM_PUSH_MULTIPLIER;
            }
            else if (moveDirection.magnitude > MIN_INPUT_DIRECTION_THRESHOLD)
            {
                // Use input direction if no momentum
                unstickMove += moveDirection * pushStrength * EDGE_INPUT_PUSH_MULTIPLIER;
            }
            
            // DON'T decay momentum as aggressively - keep the flow
            jumpMomentum *= EDGE_STUCK_MOMENTUM_DECAY;
            
            controller.Move(unstickMove);
            
            edgeStuckTimer += Time.deltaTime;
            if (edgeStuckTimer > edgeResponseTime)
            {
                verticalVelocity = Mathf.Min(verticalVelocity, edgeUnstickVelocity);
            }
        }
        
        // Apply speed cap to prevent glitches (momentum + speed combined)
        float horizontalSpeed = new Vector3(velocity.x, 0, velocity.z).magnitude;
        if (horizontalSpeed > maxCombinedSpeed)
        {
            Vector3 horizontalVelocity = new Vector3(velocity.x, 0, velocity.z);
            horizontalVelocity = horizontalVelocity.normalized * maxCombinedSpeed;
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
            horizontalSpeed = maxCombinedSpeed;
        }
        
        EventBus.Raise(new OnPlayerSpeedEvent
        {
            Player = gameObject,
            CurrentSpeed = currentSpeed,
            SlideSpeed = isCrouchSliding ? currentSpeed : 0f,
            TotalSpeed = horizontalSpeed,
            MaxSpeed = maxSpeed
        });
    }
    
    // ========================================================================
    // VISUAL ROTATION
    // ========================================================================
    
    private void UpdateVisualRotation(GroundCheckResult groundResult)
    {
        if (!alignToSlope) return;
        if (isGrabbingLedge || isClimbingLedge) return;
        
        if (groundResult.isGrounded && groundResult.angle > 5f)
        {
            // Apply slope alignment while preserving yaw rotation toward movement direction
            // This allows the character to tilt according to slope while moving
            Quaternion aligned = groundDetection.GetSlopeAlignedRotation(transform.rotation);
            transform.rotation = Quaternion.Slerp(transform.rotation, aligned, slopeAlignmentSpeed * Time.deltaTime);
        }
        else if (!groundResult.isGrounded)
        {
            // When in air, smoothly return to upright while preserving yaw
            Vector3 euler = transform.eulerAngles;
            Quaternion upright = Quaternion.Euler(0f, euler.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, upright, slopeAlignmentSpeed * 0.5f * Time.deltaTime);
        }
        else if (groundResult.isGrounded && groundResult.angle <= 5f)
        {
            // On flat ground, ensure we're upright
            Vector3 euler = transform.eulerAngles;
            Quaternion upright = Quaternion.Euler(0f, euler.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, upright, slopeAlignmentSpeed * Time.deltaTime);
        }
    }
    
    // ========================================================================
    // STATE MANAGEMENT
    // ========================================================================
    
    private void UpdateState(GroundCheckResult groundResult)
    {
        PlayerState newState = DetermineState(groundResult);
        
        if (newState != currentState)
        {
            PlayerState oldState = currentState;
            currentState = newState;
            
            EventBus.Raise(new OnPlayerStateChangeEvent
            {
                Player = gameObject,
                PreviousState = oldState,
                NewState = newState
            });
        }
    }
    
    private PlayerState DetermineState(GroundCheckResult groundResult)
    {
        if (isClimbingLedge) return PlayerState.LedgeClimbing;
        if (isGrabbingLedge) return PlayerState.LedgeGrabbing;
        if (isGroundPoundStarting || isGroundPounding) return PlayerState.GroundPounding;
        if (isDiving) return PlayerState.Diving;
        
        if (!groundResult.isGrounded)
        {
            if (isJumping)
            {
                return lastJumpType switch
                {
                    JumpType.Double => PlayerState.DoubleJumping,
                    JumpType.Triple => PlayerState.TripleJumping,
                    JumpType.Long => PlayerState.LongJumping,
                    JumpType.Backflip => PlayerState.Backflipping,
                    _ => PlayerState.Jumping
                };
            }
            return PlayerState.Falling;
        }
        
        if (isCrouchSliding) return PlayerState.CrouchSliding;
        if (groundResult.isOnSteepSlope) return PlayerState.SlopeSliding;
        
        if (isCrouching)
        {
            return currentSpeed > 0.5f ? PlayerState.CrouchWalking : PlayerState.Crouching;
        }
        
        if (currentSpeed < 0.5f) return PlayerState.Idle;
        if (currentSpeed > (minSpeed + maxSpeed) * 0.5f) return PlayerState.Running;
        return PlayerState.Walking;
    }
    
    // ========================================================================
    // DEBUG GIZMOS
    // ========================================================================
    
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Movement gizmos
        if (showMovementGizmos)
        {
            // Movement direction
            if (moveDirection.magnitude > 0.1f)
            {
                Gizmos.color = Color.blue;
                Vector3 start = transform.position + Vector3.up * 0.5f;
                Gizmos.DrawLine(start, start + moveDirection * currentSpeed * 0.2f);
                Gizmos.DrawWireSphere(start + moveDirection * currentSpeed * 0.2f, 0.1f);
            }
            
            // Target speed indicator
            Gizmos.color = Color.green;
            Vector3 speedBarStart = transform.position + Vector3.up * 2.2f + Vector3.left * 0.5f;
            float speedProgress = currentSpeed / maxSpeed;
            Gizmos.DrawLine(speedBarStart, speedBarStart + Vector3.right * speedProgress);
            
            // Acceleration progress
            Gizmos.color = Color.yellow;
            float accelProgress = Mathf.Clamp01(moveTime / accelerationTime);
            Vector3 accelBarStart = transform.position + Vector3.up * 2.4f + Vector3.left * 0.5f;
            Gizmos.DrawLine(accelBarStart, accelBarStart + Vector3.right * accelProgress);
        }
        
        // Jump gizmos
        if (showJumpGizmos)
        {
            // Jump momentum
            if (!groundDetection.IsGrounded && jumpMomentum.magnitude > 0.1f)
            {
                Gizmos.color = Color.yellow;
                Vector3 start = transform.position + Vector3.up;
                Gizmos.DrawLine(start, start + jumpMomentum * 0.2f);
            }
            
            // Jump count indicator
            if (jumpCount > 0)
            {
                Gizmos.color = jumpCount == 1 ? Color.white : (jumpCount == 2 ? Color.yellow : Color.cyan);
                for (int i = 0; i < jumpCount; i++)
                {
                    Gizmos.DrawWireSphere(transform.position + Vector3.up * (2.8f + i * 0.3f), 0.1f);
                }
            }
            
            // Triple jump window indicator
            if (jumpCount > 0 && groundDetection.IsGrounded)
            {
                float timeLeft = tripleJumpWindow - (Time.time - lastJumpTime);
                if (timeLeft > 0)
                {
                    Gizmos.color = Color.Lerp(Color.red, Color.green, timeLeft / tripleJumpWindow);
                    float windowProgress = timeLeft / tripleJumpWindow;
                    Vector3 windowBarStart = transform.position + Vector3.up * 2.6f + Vector3.left * 0.5f;
                    Gizmos.DrawLine(windowBarStart, windowBarStart + Vector3.right * windowProgress);
                }
            }
            
            // Coyote time indicator
            if (!groundDetection.IsGrounded && coyoteTimer > 0)
            {
                Gizmos.color = new Color(1f, 0.5f, 0f, coyoteTimer / coyoteTime);
                Gizmos.DrawWireSphere(transform.position, 0.3f);
            }
        }
        
        // State gizmos
        if (showStateGizmos)
        {
            Gizmos.color = currentState switch
            {
                PlayerState.Idle => Color.white,
                PlayerState.Walking => Color.green,
                PlayerState.Running => Color.cyan,
                PlayerState.Crouching => Color.yellow,
                PlayerState.CrouchSliding => Color.magenta,
                PlayerState.Jumping => Color.blue,
                PlayerState.DoubleJumping => new Color(0.5f, 0.5f, 1f),
                PlayerState.TripleJumping => new Color(0f, 1f, 1f),
                PlayerState.Falling => Color.red,
                PlayerState.GroundPounding => new Color(1f, 0.5f, 0f),
                PlayerState.LedgeGrabbing => Color.cyan,
                PlayerState.Diving => new Color(1f, 0f, 0.5f),
                _ => Color.gray
            };
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 2.5f, 0.2f);
            
            // Ground pound indicator
            if (isGroundPounding || isGroundPoundStarting)
            {
                Gizmos.color = isGroundPoundStarting ? Color.yellow : Color.red;
                Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.5f, 0.5f);
            }
        }
        
        // Collider gizmos
        if (showColliderGizmos && controller != null)
        {
            if (crouchLocked)
                Gizmos.color = Color.red;
            else if (isCrouching)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.white;
            
            Vector3 center = transform.position + controller.center;
            Vector3 bottom = center + Vector3.down * (controller.height * 0.5f - controller.radius);
            Vector3 top = center + Vector3.up * (controller.height * 0.5f - controller.radius);
            Gizmos.DrawWireSphere(bottom, controller.radius);
            Gizmos.DrawWireSphere(top, controller.radius);
            Gizmos.DrawLine(bottom + Vector3.left * controller.radius, top + Vector3.left * controller.radius);
            Gizmos.DrawLine(bottom + Vector3.right * controller.radius, top + Vector3.right * controller.radius);
            Gizmos.DrawLine(bottom + Vector3.forward * controller.radius, top + Vector3.forward * controller.radius);
            Gizmos.DrawLine(bottom + Vector3.back * controller.radius, top + Vector3.back * controller.radius);
        }
        
        // Ledge gizmos
        if (showLedgeGizmos)
        {
            if (!groundDetection.IsGrounded && !isGrabbingLedge)
            {
                Gizmos.color = Color.cyan;
                Vector3 checkOrigin = transform.position + Vector3.up * (currentHeight - ledgeGrabHeight);
                Gizmos.DrawLine(checkOrigin, checkOrigin + transform.forward * ledgeGrabDistance);
            }
            
            if (isGrabbingLedge)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(ledgePosition, 0.2f);
                Gizmos.DrawLine(ledgePosition, ledgePosition + ledgeNormal);
            }
        }
    }
}
