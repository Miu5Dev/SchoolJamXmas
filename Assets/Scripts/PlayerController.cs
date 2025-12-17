using UnityEngine;

public class PlayerController : MonoBehaviour
{
 [Header("Movement")]
    [SerializeField] private float minSpeed = 4f;
    [SerializeField] private float maxSpeed = 12f;
    [SerializeField] private float speedBuildupTime = 2f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 12f;
    [SerializeField] private float rotationSpeed = 15f;
    
    [Header("Jumping")]
    [SerializeField] private float jumpHeight = 2.5f;
    [SerializeField] private float doubleJumpHeight = 3f;
    [SerializeField] private float tripleJumpHeight = 4.5f;
    [SerializeField] private float longJumpHeight = 2f;
    [SerializeField] private float longJumpDistance = 15f;
    [SerializeField] private float backflipHeight = 4f;
    [SerializeField] private float sideflipHeight = 3.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpChainWindow = 0.5f;
    [Tooltip("How much horizontal momentum is preserved when jumping (0 = none, 1 = full)")]
    [Range(0f, 1f)]
    [SerializeField] private float jumpMomentumRetention = 0.95f;
    
    [Header("Wall Collision")]
    [SerializeField] private float wallBonkSpeedThreshold = 8f;
    [SerializeField] private float wallBonkKnockback = 3f;
    [SerializeField] private float wallBonkStunDuration = 0.2f;
    [SerializeField] private float wallSlowdownFactor = 0.3f;
    
    [Header("Bonk Settings")]
    [SerializeField] private float bonkKnockbackForce = 5f;
    [SerializeField] private float bonkStunDuration = 0.3f;
    
    [Header("Ground Pound")]
    [SerializeField] private float groundPoundForce = 30f;
    [SerializeField] private float groundPoundJumpHeight = 4.2f;
    [SerializeField] private float groundPoundJumpWindow = 0.8f;
    [SerializeField] private float groundPoundMovementThreshold = 0.3f;
    [SerializeField] private float groundPoundAimSpeed = 3f;
    [Tooltip("Minimum height above ground required to perform ground pound")]
    [SerializeField] private float groundPoundMinHeight = 2f; // NEW
    [Tooltip("Maximum raycast distance to check ground height")]
    [SerializeField] private float groundPoundHeightCheckDistance = 50f; // NEW
    [Tooltip("Horizontal speed multiplier for ground pound jump")]
    [Range(0.3f, 1.5f)]
    [SerializeField] private float groundPoundJumpSpeedMultiplier = 0.7f;
    
    [Header("Dive Settings")]
    [SerializeField] private float diveRecoveryTime = 0.3f;
    [SerializeField] private float diveHorizontalSpeed = 16f;
    [SerializeField] private float diveGravityMultiplier = 0.3f;
    [SerializeField] private float diveInitialLift = 3f;
    [Tooltip("Speed retention on hard dive landing (before friction)")]
    [Range(0.05f, 0.5f)]
    [SerializeField] private float diveHardLandingRetention = 0.15f;
    [Tooltip("Speed retention on soft dive landing (before friction)")]
    [Range(0.2f, 0.8f)]
    [SerializeField] private float diveSoftLandingRetention = 0.4f;
    [Tooltip("Deceleration multiplier during dive slide recovery")]
    [Range(0.1f, 2f)]
    [SerializeField] private float diveSlideDeceleration = 0.3f;
    
    [Header("Air Control")]
    [SerializeField] private float airAcceleration = 6f;
    [SerializeField] private float airDeceleration = 2f;
    [Tooltip("How much you can change direction mid-air (0 = locked, 1 = full control)")]
    [Range(0f, 1f)]
    [SerializeField] private float airControlAmount = 0.7f;
    
    [Header("Slopes")]
    [SerializeField] private float slopeCheckDistance = 0.8f;
    [SerializeField] private float maxSlideSpeed = 12f;
    [SerializeField] private float slopeSlideAcceleration = 8f;
    [SerializeField] private float slopeClimbSpeedThreshold = 5f;
    [SerializeField] private float slopeStickForce = 8f;
    [SerializeField] private int slopeRaycastCount = 4;
    
    [Header("Surface Detection")]
    [SerializeField] private LayerMask surfaceDetectionMask = -1;
    [SerializeField] private float surfaceCheckRadius = 0.3f;
    
    [Header("Inertia Settings")]
    [Tooltip("Minimum speed threshold to maintain momentum (prevents tiny drifts)")]
    [SerializeField] private float minimumMomentumThreshold = 0.1f;
    [Tooltip("How much momentum is preserved when changing direction")]
    [Range(0f, 1f)]
    [SerializeField] private float directionChangeMomentumRetention = 0.5f;
    [Tooltip("Smooth landing - preserve speed when landing from jumps")]
    [Range(0.7f, 1f)]
    [SerializeField] private float landingMomentumRetention = 0.95f;
    
    [Header("Input Settings")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string verticalAxis = "Vertical";
    [SerializeField] private string jumpButton = "Jump";
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
    [SerializeField] private KeyCode diveKey = KeyCode.C;
    
    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    
    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float coyoteTimeCounter;
    private bool isGrounded;
    private bool wasGroundedLastFrame;
    private RaycastHit slopeHit;
    private bool isOnSlope = false;
    private bool isOnSteepSlope = false;
    private float currentSlideSpeed = 0f;
    private float bonkStunTimer = 0f;
    private float diveRecoveryTimer = 0f;
    
    // Speed buildup system
    private float currentMaxSpeed;
    private float speedBuildupTimer = 0f;
    
    // Jump chain tracking
    private int jumpCount = 0;
    private float jumpChainTimer = 0f;
    
    // Ground pound jump timing
    private bool canGroundPoundJump = false;
    private float groundPoundJumpTimer = 0f;
    private Vector3 groundPoundLandPosition;
    private Vector3 groundPoundJumpDirection;
    
    // Surface properties
    private SurfaceMaterial currentSurface;
    private float currentFriction = 1f;
    private float currentAccelMult = 1f;
    private float currentDecelMult = 1f;
    private float currentSpeedMult = 1f;
    
    // Movement state
    private Vector3 lastMoveDirection;
    private bool isGroundPounding = false;
    private bool isDiving = false;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
        
        currentMaxSpeed = minSpeed;
    }
    
    void Update()
    {
        wasGroundedLastFrame = isGrounded;
        
        HandleBonkStun();
        HandleGroundPoundLanding();
        HandleGroundPoundJumpWindow();
        HandleGroundCheck();
        DetectSurface();
        HandleJumpChainTimer();
        HandleSlopes();
        HandleBonkCollision();
        HandleWallCollision();
        
        if (!isGroundPounding && !isDiving && bonkStunTimer <= 0f)
        {
            HandleMovement();
            HandleJumps();
            HandleDive();
        }
        
        HandleGroundPound();
        ApplyGravity();
        
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;
        
        if (isOnSlope && !isOnSteepSlope && isGrounded && moveDirection.magnitude > 0.1f)
        {
            finalMovement += Vector3.down * slopeStickForce;
        }
        
        controller.Move(finalMovement * Time.deltaTime);
    }
    
    void HandleBonkStun()
    {
        if (bonkStunTimer > 0f)
        {
            bonkStunTimer -= Time.deltaTime;
            moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, 5f * Time.deltaTime);
        }
    }
    
    void HandleBonkCollision()
    {
        if ((controller.collisionFlags & CollisionFlags.Above) != 0)
        {
            if (verticalVelocity > 0f)
            {
                verticalVelocity = -bonkKnockbackForce;
                bonkStunTimer = bonkStunDuration;
                moveDirection -= transform.forward * bonkKnockbackForce * 0.5f;
                jumpCount = 0;
                jumpChainTimer = 0f;
                
                Debug.Log("CEILING BONK!");
            }
        }
    }
    
    void HandleWallCollision()
    {
        if ((controller.collisionFlags & CollisionFlags.Sides) != 0)
        {
            float currentSpeed = moveDirection.magnitude;
            
            if (!isGrounded)
            {
                if (currentSpeed >= wallBonkSpeedThreshold)
                {
                    Vector3 bounceDirection = -moveDirection.normalized;
                    moveDirection = bounceDirection * wallBonkKnockback;
                    
                    bonkStunTimer = wallBonkStunDuration;
                    speedBuildupTimer = 0f;
                    currentMaxSpeed = minSpeed;
                    
                    if (verticalVelocity > 0)
                    {
                        verticalVelocity *= 0.5f;
                    }
                    
                    Debug.Log("WALL BONK!");
                }
                else if (currentSpeed > 1f)
                {
                    moveDirection *= wallSlowdownFactor;
                }
            }
            else
            {
                if (currentSpeed > 1f)
                {
                    moveDirection *= wallSlowdownFactor;
                    speedBuildupTimer *= 0.5f;
                }
                else
                {
                    moveDirection = Vector3.zero;
                    speedBuildupTimer = 0f;
                }
            }
        }
    }
    
    void HandleGroundPoundLanding()
    {
        if (isGroundPounding && controller.isGrounded)
        {
            canGroundPoundJump = true;
            groundPoundJumpTimer = groundPoundJumpWindow;
            groundPoundLandPosition = transform.position;
            groundPoundJumpDirection = Vector3.zero;
            isGroundPounding = false;
            Debug.Log("Ground Pound Landed! Aim with WASD and press jump!");
        }
    }
    
    void HandleGroundPoundJumpWindow()
    {
        if (canGroundPoundJump)
        {
            groundPoundJumpTimer -= Time.deltaTime;
            
            if (groundPoundJumpTimer <= 0f)
            {
                canGroundPoundJump = false;
                Debug.Log("Ground Pound Jump window expired!");
                return;
            }
            
            if (Input.GetButtonDown(jumpButton))
            {
                return;
            }
            
            float horizontal = Input.GetAxisRaw(horizontalAxis);
            float vertical = Input.GetAxisRaw(verticalAxis);
            
            if (Mathf.Abs(horizontal) > 0.1f || Mathf.Abs(vertical) > 0.1f)
            {
                Vector3 forward = cameraTransform.forward;
                Vector3 right = cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();
                
                groundPoundJumpDirection = (forward * vertical + right * horizontal).normalized;
                
                if (groundPoundJumpDirection.magnitude > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(groundPoundJumpDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                        rotationSpeed * 0.5f * Time.deltaTime);
                }
                
                Vector3 aimMovement = groundPoundJumpDirection * groundPoundAimSpeed * Time.deltaTime;
                controller.Move(aimMovement);
                
                float distanceMoved = Vector3.Distance(transform.position, groundPoundLandPosition);
                if (distanceMoved > groundPoundMovementThreshold)
                {
                    canGroundPoundJump = false;
                    groundPoundJumpTimer = 0f;
                    groundPoundJumpDirection = Vector3.zero;
                    Debug.Log("Ground Pound Jump cancelled - moved too far!");
                }
            }
        }
    }
    
    void HandleJumpChainTimer()
    {
        if (isGrounded && jumpCount > 0)
        {
            jumpChainTimer -= Time.deltaTime;
            
            if (jumpChainTimer <= 0f)
            {
                jumpCount = 0;
                jumpChainTimer = 0f;
            }
        }
    }
    
    void DetectSurface()
    {
        currentFriction = 1f;
        currentAccelMult = 1f;
        currentDecelMult = 1f;
        currentSpeedMult = 1f;
        currentSurface = null;
        
        if (!isGrounded)
            return;
        
        Vector3 basePosition = transform.position;
        RaycastHit hit;
        
        if (Physics.Raycast(basePosition, Vector3.down, out hit, 
            controller.height / 2 + slopeCheckDistance, surfaceDetectionMask))
        {
            SurfaceMaterial surface = hit.collider.GetComponent<SurfaceMaterial>();
            
            if (surface != null)
            {
                currentSurface = surface;
                currentFriction = surface.Friction;
                currentAccelMult = surface.AccelerationMultiplier;
                currentDecelMult = surface.DecelerationMultiplier;
                currentSpeedMult = surface.MaxSpeedMultiplier;
            }
        }
    }
    
    void HandleGroundCheck()
    {
        bool wasGrounded = isGrounded;
        isGrounded = controller.isGrounded;
        
        if (diveRecoveryTimer > 0f)
        {
            diveRecoveryTimer -= Time.deltaTime;
        }
        
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            
            if (!wasGrounded)
            {
                // Just landed from being airborne
                if (isDiving)
                {
                    // Dive landing
                    float diveSpeed = moveDirection.magnitude;
                    
                    float baseRetention;
                    if (diveSpeed > minSpeed * 2f)
                    {
                        baseRetention = diveHardLandingRetention;
                        diveRecoveryTimer = diveRecoveryTime;
                    }
                    else
                    {
                        baseRetention = diveSoftLandingRetention;
                        diveRecoveryTimer = diveRecoveryTime * 0.5f;
                    }
                    
                    float frictionAdjustedRetention = baseRetention / currentFriction;
                    frictionAdjustedRetention = Mathf.Clamp(frictionAdjustedRetention, 0.05f, 0.8f);
                    
                    moveDirection *= frictionAdjustedRetention;
                    
                    diveRecoveryTimer *= (2f - currentFriction);
                    diveRecoveryTimer = Mathf.Max(0.1f, diveRecoveryTimer);
                    
                    Debug.Log($"Dive Landing on {(currentSurface != null ? currentSurface.SurfaceType.ToString() : "Normal")} - Speed Retention: {frictionAdjustedRetention:F2}");
                }
                else if (!isGroundPounding)
                {
                    // Normal landing - preserve horizontal momentum
                    moveDirection *= landingMomentumRetention;
                }
                
                isDiving = false;
                currentSlideSpeed = 0f;
                bonkStunTimer = 0f;
                
                if (jumpCount > 0)
                {
                    jumpChainTimer = jumpChainWindow;
                }
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
            
            if (wasGrounded && !Input.GetButton(jumpButton))
            {
                canGroundPoundJump = false;
                groundPoundJumpTimer = 0f;
            }
        }
    }
    
    void HandleSlopes()
    {
        isOnSlope = false;
        isOnSteepSlope = false;
        float averageSlopeAngle = 0f;
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        
        float radius = controller.radius;
        Vector3 basePosition = transform.position;
        
        for (int i = 0; i < slopeRaycastCount; i++)
        {
            float angle = (360f / slopeRaycastCount) * i;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 rayOrigin = basePosition + direction * radius * 0.9f;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 
                controller.height / 2 + slopeCheckDistance))
            {
                float slopeAngle = Vector3.Angle(Vector3.up, hit.normal);
                averageSlopeAngle += slopeAngle;
                averageNormal += hit.normal;
                hitCount++;
                slopeHit = hit;
            }
        }
        
        if (hitCount > 0)
        {
            averageSlopeAngle /= hitCount;
            averageNormal = (averageNormal / hitCount).normalized;
            
            if (averageSlopeAngle > controller.slopeLimit && isGrounded)
            {
                isOnSlope = true;
                isOnSteepSlope = true;
                
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, averageNormal).normalized;
                float currentSpeed = moveDirection.magnitude;
                float upSlopeAlignment = Vector3.Dot(moveDirection.normalized, -slideDirection);
                
                if (upSlopeAlignment > 0.5f && currentSpeed >= slopeClimbSpeedThreshold)
                {
                    float speedLoss = slopeSlideAcceleration * Time.deltaTime;
                    float newSpeed = Mathf.Max(0, currentSpeed - speedLoss);
                    
                    if (newSpeed > 0)
                    {
                        Vector3 projectedMove = Vector3.ProjectOnPlane(moveDirection, averageNormal).normalized;
                        moveDirection = projectedMove * newSpeed;
                        return;
                    }
                }
                
                float targetSlideSpeed = maxSlideSpeed * (averageSlopeAngle / 90f);
                
                if (currentSurface != null && currentSurface.AffectsSliding)
                {
                    targetSlideSpeed *= currentSurface.SlideSpeedMultiplier;
                }
                
                currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, targetSlideSpeed, 
                    slopeSlideAcceleration * Time.deltaTime);
                
                moveDirection = slideDirection * currentSlideSpeed;
            }
            else if (averageSlopeAngle > 5f && averageSlopeAngle < controller.slopeLimit)
            {
                isOnSlope = true;
                isOnSteepSlope = false;
                
                if (moveDirection.magnitude > 0.1f)
                {
                    Vector3 projectedMove = Vector3.ProjectOnPlane(moveDirection, averageNormal);
                    moveDirection = projectedMove.normalized * moveDirection.magnitude;
                }
                currentSlideSpeed = 0f;
            }
            else
            {
                currentSlideSpeed = 0f;
            }
        }
    }
    
    void HandleMovement()
    {
        if (canGroundPoundJump)
            return;
        
        if (diveRecoveryTimer > 0f && isGrounded)
        {
            // During dive recovery, maintain momentum with gentle deceleration
            float slideDecel = deceleration * currentFriction * diveSlideDeceleration;
            moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 
                slideDecel * Time.deltaTime);
            
            // Keep character facing movement direction during slide
            if (moveDirection.magnitude > 0.5f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                    rotationSpeed * 0.3f * Time.deltaTime);
            }
            return;
        }
            
        if (isOnSteepSlope)
        {
            speedBuildupTimer = 0f;
            currentMaxSpeed = minSpeed;
            return;
        }
            
        float horizontal = Input.GetAxisRaw(horizontalAxis);
        float vertical = Input.GetAxisRaw(verticalAxis);
        Vector3 inputDir = new Vector3(horizontal, 0f, vertical).normalized;
        
        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        
        Vector3 targetDirection = (forward * inputDir.z + right * inputDir.x);
        
        // Air control - blend input with current momentum
        float currentAccel = isGrounded ? acceleration * currentAccelMult : airAcceleration;
        float currentDecel = isGrounded ? deceleration * currentDecelMult * currentFriction : airDeceleration;
        
        if (targetDirection.magnitude > 0.1f)
        {
            // Check for sharp direction changes
            if (lastMoveDirection.magnitude > 0.1f)
            {
                float directionDifference = Vector3.Dot(targetDirection, lastMoveDirection);
                if (directionDifference < 0.5f)
                {
                    speedBuildupTimer = 0f;
                    // Apply momentum retention on direction change
                    if (isGrounded)
                    {
                        moveDirection *= directionChangeMomentumRetention;
                    }
                }
            }
            
            if (isGrounded)
            {
                speedBuildupTimer += Time.deltaTime;
                float speedProgress = Mathf.Clamp01(speedBuildupTimer / speedBuildupTime);
                currentMaxSpeed = Mathf.Lerp(minSpeed, maxSpeed * currentSpeedMult, speedProgress);
            }
            
            Vector3 targetVelocity = targetDirection * currentMaxSpeed;
            
            // In air, blend with existing momentum for smooth control
            if (!isGrounded)
            {
                targetVelocity = Vector3.Lerp(moveDirection, targetVelocity, airControlAmount);
            }
            
            moveDirection = Vector3.MoveTowards(moveDirection, targetVelocity, 
                currentAccel * Time.deltaTime);
            
            lastMoveDirection = targetDirection;
            
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 
                rotationSpeed * Time.deltaTime);
        }
        else
        {
            // No input - decelerate but preserve some momentum in air
            if (isGrounded)
            {
                speedBuildupTimer = 0f;
                currentMaxSpeed = minSpeed;
                
                moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 
                    currentDecel * Time.deltaTime);
            }
            else
            {
                // In air with no input - maintain momentum with minimal deceleration
                moveDirection = Vector3.MoveTowards(moveDirection, Vector3.zero, 
                    currentDecel * 0.3f * Time.deltaTime);
            }
        }
        
        // Clean up tiny movements
        if (moveDirection.magnitude < minimumMomentumThreshold)
        {
            moveDirection = Vector3.zero;
        }
    }
    
    void HandleJumps()
    {
        if (diveRecoveryTimer > 0f)
            return;
        
        if (canGroundPoundJump && Input.GetButtonDown(jumpButton) && isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(groundPoundJumpHeight * -2f * gravity);
            
            if (groundPoundJumpDirection.magnitude > 0.1f)
            {
                moveDirection = groundPoundJumpDirection * maxSpeed * groundPoundJumpSpeedMultiplier;
            }
            else
            {
                moveDirection = Vector3.zero;
            }
            
            canGroundPoundJump = false;
            groundPoundJumpTimer = 0f;
            groundPoundJumpDirection = Vector3.zero;
            jumpCount = 0;
            jumpChainTimer = 0f;
            Debug.Log("GROUND POUND JUMP!");
            return;
        }
        
        if (Input.GetButtonDown(jumpButton) && coyoteTimeCounter > 0f)
        {
            canGroundPoundJump = false;
            groundPoundJumpTimer = 0f;
            
            Vector3 cameraBackward = -cameraTransform.forward;
            cameraBackward.y = 0f;
            cameraBackward.Normalize();
            
            float currentSpeed = moveDirection.magnitude;
            bool isMovingFast = currentSpeed > minSpeed + 1f;
            
            // Store current momentum
            Vector3 currentMomentum = moveDirection;
            
            if (Input.GetKey(crouchKey) && isMovingFast)
            {
                // Long Jump - use forward direction with current speed
                verticalVelocity = Mathf.Sqrt(longJumpHeight * -2f * gravity);
                moveDirection = transform.forward * longJumpDistance;
                jumpCount = 0;
                jumpChainTimer = 0f;
            }
            else if (Input.GetKey(crouchKey))
            {
                // Backflip
                verticalVelocity = Mathf.Sqrt(backflipHeight * -2f * gravity);
                moveDirection = cameraBackward * (maxSpeed * 0.5f);
                
                if (cameraBackward.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.LookRotation(cameraBackward);
                }
                jumpCount = 0;
                jumpChainTimer = 0f;
            }
            else if (Vector3.Dot(lastMoveDirection, transform.forward) < -0.7f && 
                     currentSpeed > minSpeed * 0.5f)
            {
                // Sideflip - preserve momentum
                verticalVelocity = Mathf.Sqrt(sideflipHeight * -2f * gravity);
                moveDirection = currentMomentum * jumpMomentumRetention;
                jumpCount = 0;
                jumpChainTimer = 0f;
            }
            else if (jumpCount == 2 && currentSpeed >= minSpeed)
            {
                // Triple Jump - preserve momentum
                verticalVelocity = Mathf.Sqrt(tripleJumpHeight * -2f * gravity);
                moveDirection = currentMomentum * jumpMomentumRetention;
                jumpCount = 0;
                jumpChainTimer = 0f;
            }
            else if (jumpCount == 1)
            {
                // Double Jump - preserve momentum
                verticalVelocity = Mathf.Sqrt(doubleJumpHeight * -2f * gravity);
                moveDirection = currentMomentum * jumpMomentumRetention;
                jumpCount = 2;
                jumpChainTimer = jumpChainWindow;
            }
            else
            {
                // Single Jump - preserve momentum
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                moveDirection = currentMomentum * jumpMomentumRetention;
                jumpCount = 1;
                jumpChainTimer = jumpChainWindow;
            }
            
            coyoteTimeCounter = 0f;
        }
    }
    
    void HandleDive()
    {
        if (Input.GetKeyDown(diveKey) && !isGrounded)
        {
            isDiving = true;
            moveDirection = transform.forward * diveHorizontalSpeed;
            verticalVelocity = diveInitialLift;
            jumpCount = 0;
            jumpChainTimer = 0f;
            Debug.Log("DIVE!");
        }
    }
    
    void HandleGroundPound()
    {
        if (Input.GetKeyDown(crouchKey) && !isGrounded && !isGroundPounding)
        {
            // Check if we're high enough above ground
            RaycastHit hit;
            float distanceToGround = float.MaxValue;
        
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 
                    groundPoundHeightCheckDistance, surfaceDetectionMask))
            {
                distanceToGround = hit.distance;
            }
        
            // Only allow ground pound if above minimum height
            if (distanceToGround >= groundPoundMinHeight)
            {
                isGroundPounding = true;
                verticalVelocity = -groundPoundForce;
                moveDirection = Vector3.zero;
                jumpCount = 0;
                jumpChainTimer = 0f;
                Debug.Log("Ground Pound Started!");
            }
            else
            {
                Debug.Log($"Too low for Ground Pound! Height: {distanceToGround:F2}m (need {groundPoundMinHeight}m)");
            }
        }
    }
    
    void ApplyGravity()
    {
        if (isGrounded && verticalVelocity < 0f && !isGroundPounding)
        {
            verticalVelocity = -2f;
        }
        else
        {
            // Apply reduced gravity while diving for that "glide" feel
            if (isDiving)
            {
                verticalVelocity += gravity * diveGravityMultiplier * Time.deltaTime;
            }
            else
            {
                verticalVelocity += gravity * Time.deltaTime;
            }
        }
    }
}