using System;
using UnityEngine;

public class MovementSystem : MonoBehaviour
{
    private Vector2 InputDirection;
    private float currentSpeed;
    
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 12f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 40f;
    [SerializeField] private float turnSpeed = 720f;
    
    [Header("Camera Reference")]
    [SerializeField] private Transform cameraTransform;
    
    [Header("Ground Settings")]
    [SerializeField] private float groundCheckDistance = 0.5f;
    [SerializeField] private float groundStickForceMin = 5f;
    [SerializeField] private float groundStickForceMax = 30f;
    [SerializeField] private float groundStickSpeedReference = 40f;
    [SerializeField] private LayerMask groundLayer = ~0;
    
    [Header("Slope Settings")]
    [SerializeField] private float maxWalkableAngle = 45f;
    [SerializeField] private float slideAccelerationMin = 3f;
    [SerializeField] private float slideAccelerationMax = 12f;
    [SerializeField] private float slidePushbackMultiplier = 1.5f;
    [SerializeField] private float slideControlFactor = 0.6f;
    [SerializeField] private float slideLateralAcceleration = 20f;
    [SerializeField] private float struggleStrength = 0.6f;
    [SerializeField] private float struggleDecay = 2f;
    
    [Header("Slide Activation Settings")]
    [SerializeField] private float slideActivationDelay = 0.4f;
    [SerializeField] private float slideActivationRampTime = 0.5f;
    
    [Header("Slide Momentum Settings")]
    [SerializeField] private float slideMomentumDecay = 5f;
    [SerializeField] private float slideCancelThreshold = 0.5f;
    [SerializeField] private float slideMinSpeedThreshold = 8f;
    
    [Header("Momentum Settings")]
    [SerializeField] private float momentumDecay = 3f;
    [SerializeField] private float momentumInfluence = 0.8f;
    
    [Header("Crouch Settings")]
    [SerializeField] private float normalHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float normalRadius = 0.5f;
    [SerializeField] private float crouchRadius = 0.5f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float crouchSlideBoost = 2f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float doubleJumpForce = 10f;
    [SerializeField] private float tripleJumpForce = 15f;
    [SerializeField] private float longJumpForce = 8f;
    [SerializeField] private float longJumpHorizontalBoost = 15f;
    [SerializeField] private float backflipForce = 14f;
    [SerializeField] private float backflipHorizontalForce = 3f;
    [SerializeField] private float groundPoundJumpForce = 18f;
    [SerializeField] private float slopeJumpForceMultiplier = 1.2f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private float tripleJumpWindow = 0.5f;
    [SerializeField] private float tripleJumpSpeedRequirement = 8f;
    [SerializeField] private float airControl = 0.6f;
    [SerializeField] private float jumpCutMultiplier = 0.5f;
    
    [Header("Ground Pound Settings")]
    [SerializeField] private float groundPoundDelay = 0.2f;
    [SerializeField] private float groundPoundSpeed = 30f;
    [SerializeField] private float groundPoundJumpWindow = 0.3f;
    
    [Header("Ledge Grab Settings")]
    [SerializeField] private float ledgeGrabDistance = 0.6f;
    [SerializeField] private float ledgeGrabHeight = 0.5f;
    [SerializeField] private float ledgeClimbDuration = 0.3f;
    [SerializeField] private float ledgeJumpForce = 10f;
    [SerializeField] private float ledgeJumpHorizontalForce = 6f;
    [SerializeField] private LayerMask ledgeLayer = ~0;
    
    [Header("State")]
    [SerializeField] private bool running = false;
    [SerializeField] private bool crouching = false;
    [SerializeField] private bool crouchInputHeld = false;
    [SerializeField] private bool jumpInputHeld = false;
    [SerializeField] private bool crouchLocked = false;
    [SerializeField] private bool isGrounded = false;
    [SerializeField] private bool isSliding = false;
    [SerializeField] private bool isCrouchSliding = false;
    [SerializeField] private bool isOnSteepSlope = false;
    [SerializeField] private bool hasSlideMomentum = false;
    [SerializeField] private float currentSlopeAngle;
    
    [Header("Jump State")]
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool canDoubleJump = false;
    [SerializeField] private int jumpCount = 0;
    [SerializeField] private JumpType lastJumpType = JumpType.Normal;
    [SerializeField] private bool isGroundPounding = false;
    [SerializeField] private bool isGroundPoundStarting = false;
    [SerializeField] private bool canGroundPoundJump = false;
    [SerializeField] private bool isGrabbingLedge = false;
    [SerializeField] private bool isClimbingLedge = false;
    
    [Header("Debug")]
    [SerializeField] private float currentSlideSpeed;
    [SerializeField] private float currentSlideAcceleration;
    [SerializeField] private float currentGroundStickForce;
    [SerializeField] private float totalSpeed;
    [SerializeField] private float uphillAmount;
    [SerializeField] private float struggleMeter;
    [SerializeField] private float steepSlopeTimer;
    [SerializeField] private float slideInfluence;
    [SerializeField] private float coyoteTimer;
    [SerializeField] private float jumpBufferTimer;
    [SerializeField] private float lastGroundedTime;
    [SerializeField] private float lastJumpTime;
    [SerializeField] private float groundPoundTimer;
    [SerializeField] private float groundPoundLandTime;
    [SerializeField] private float ledgeClimbTimer;
    [SerializeField] private Vector3 slideMomentumDirection;
    [SerializeField] private Vector3 currentMomentum;
    [SerializeField] private Vector3 jumpMomentum;
    [SerializeField] private Vector3 lateralVelocity;
    [SerializeField] private Vector3 ledgePosition;
    [SerializeField] private Vector3 ledgeClimbTarget;
    [SerializeField] private string groundHitName;
    
    private bool wasGrounded;
    private bool wasSliding;
    private bool wasCrouchSliding;
    private bool wasCrouchLocked;
    private bool wasJumpPressed;
    private bool wasCrouchPressed;
    private float lastSlopeAngle;
    
    private CharacterController controller;
    private float gravity = -35f;
    private float verticalVelocity;
    private Vector3 moveDirection;
    private Vector3 slopeNormal = Vector3.up;
    private Vector3 smoothedSlopeNormal = Vector3.up;
    private Vector3 slideVelocity;
    private Vector3 rayOrigin;
    private float rayLength;
    private float currentHeight;
    private float currentRadius;
    private Vector3 ledgeClimbStartPos;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        controller.slopeLimit = 90f;
        currentHeight = normalHeight;
        currentRadius = normalRadius;
        
        if (cameraTransform == null)
            cameraTransform = Camera.main.transform;
    }

    void OnEnable()
    {
        EventBus.Subscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Subscribe<onCrouchInputEvent>(OnCrouchInput);
        EventBus.Subscribe<onRunInputEvent>(OnRunInput);
    }

    void OnDisable()
    {
        EventBus.Unsubscribe<onMoveInputEvent>(OnMoveInput);
        EventBus.Unsubscribe<onJumpInputEvent>(OnJumpInput);
        EventBus.Unsubscribe<onCrouchInputEvent>(OnCrouchInput);
        EventBus.Unsubscribe<onRunInputEvent>(OnRunInput);
    }

    private void OnMoveInput(onMoveInputEvent ev)
    {
        InputDirection = ev.Direction;
    }

    private void OnRunInput(onRunInputEvent ev)
    {
        running = ev.pressed;
    }

    private void OnJumpInput(onJumpInputEvent ev)
    {
        jumpInputHeld = ev.pressed;
    }

    private void OnCrouchInput(onCrouchInputEvent ev)
    {
        crouchInputHeld = ev.pressed;
    }

    private void Update()
    {
        if (isClimbingLedge)
        {
            HandleLedgeClimb();
            return;
        }
        
        CheckGround();
        CheckLedgeGrab();
        
        if (isGrabbingLedge)
        {
            HandleLedgeGrab();
            return;
        }
        
        UpdateTimers();
        UpdateSlopeState();
        UpdateSlideMomentum();
        HandleCrouchLock();
        HandleCrouch();
        HandleMovement();
        HandleJump();
        HandleGroundPound();
        HandleMomentum();
        ApplyGravity();
        ApplyFinalMovement();
        RaiseEvents();
        
        wasJumpPressed = jumpInputHeld;
        wasCrouchPressed = crouchInputHeld;
    }

    private void CheckGround()
    {
        rayOrigin = transform.position + Vector3.up * 0.1f;
        rayLength = 0.1f + groundCheckDistance;
        
        Vector3 averageNormal = Vector3.zero;
        int hitCount = 0;
        float closestDistance = float.MaxValue;
        string hitName = "NONE";
        
        if (CastGroundRay(rayOrigin, out RaycastHit centerHit))
        {
            averageNormal += centerHit.normal;
            hitCount++;
            if (centerHit.distance < closestDistance)
            {
                closestDistance = centerHit.distance;
                hitName = centerHit.collider.gameObject.name;
            }
        }
        
        float offset = controller.radius * 0.5f;
        Vector3[] offsets = new Vector3[]
        {
            Vector3.forward * offset,
            Vector3.back * offset,
            Vector3.left * offset,
            Vector3.right * offset
        };
        
        foreach (Vector3 off in offsets)
        {
            if (CastGroundRay(rayOrigin + off, out RaycastHit hit))
            {
                averageNormal += hit.normal;
                hitCount++;
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    hitName = hit.collider.gameObject.name;
                }
            }
        }
        
        bool wasGroundedThisFrame = isGrounded;
        
        if (hitCount > 0)
        {
            slopeNormal = (averageNormal / hitCount).normalized;
            smoothedSlopeNormal = Vector3.Lerp(smoothedSlopeNormal, slopeNormal, 15f * Time.deltaTime);
            currentSlopeAngle = Vector3.Angle(Vector3.up, smoothedSlopeNormal);
            isGrounded = true;
            groundHitName = hitName;
            
            if (!wasGroundedThisFrame)
            {
                OnLand();
            }
        }
        else
        {
            isGrounded = false;
            slopeNormal = Vector3.up;
            smoothedSlopeNormal = Vector3.Lerp(smoothedSlopeNormal, Vector3.up, 10f * Time.deltaTime);
            currentSlopeAngle = 0f;
            groundHitName = "NONE";
        }
    }

    private void OnLand()
    {
        float fallSpeed = Mathf.Abs(verticalVelocity);
        
        if (isGroundPounding)
        {
            isGroundPounding = false;
            isGroundPoundStarting = false;
            canGroundPoundJump = true;
            groundPoundLandTime = Time.time;
            
            EventBus.Raise(new OnPlayerGroundPoundEvent
            {
                Player = gameObject,
                IsStarting = false,
                IsLanding = true
            });
        }
        
        // Reset jump si no es ground pound jump window
        if (!canGroundPoundJump)
        {
            jumpCount = 0;
            isJumping = false;
        }
        
        EventBus.Raise(new OnPlayerLandEvent
        {
            Player = gameObject,
            FallSpeed = fallSpeed,
            HardLanding = fallSpeed > 20f
        });
    }

    private void CheckLedgeGrab()
    {
        if (isGrounded || isGrabbingLedge || isClimbingLedge || verticalVelocity > 0) return;
        if (isGroundPounding) return;
        
        Vector3 forwardDir = transform.forward;
        Vector3 checkOrigin = transform.position + Vector3.up * (currentHeight - ledgeGrabHeight);
        
        // Check si hay pared adelante
        if (Physics.Raycast(checkOrigin, forwardDir, out RaycastHit wallHit, ledgeGrabDistance, ledgeLayer))
        {
            // Check si hay espacio arriba de la pared
            Vector3 ledgeCheckOrigin = wallHit.point + forwardDir * 0.1f + Vector3.up * ledgeGrabHeight;
            
            if (Physics.Raycast(ledgeCheckOrigin, Vector3.down, out RaycastHit ledgeHit, ledgeGrabHeight + 0.2f, ledgeLayer))
            {
                // Check si hay espacio para pararse
                Vector3 standCheckOrigin = ledgeHit.point + Vector3.up * (currentHeight * 0.5f + 0.1f);
                
                if (!Physics.CheckSphere(standCheckOrigin, currentRadius * 0.8f, ledgeLayer))
                {
                    GrabLedge(ledgeHit.point, wallHit.normal);
                }
            }
        }
    }

    private void GrabLedge(Vector3 ledgePoint, Vector3 wallNormal)
    {
        isGrabbingLedge = true;
        ledgePosition = ledgePoint;
        verticalVelocity = 0f;
        jumpMomentum = Vector3.zero;
        
        // Posiciona al jugador en el ledge
        Vector3 hangPosition = ledgePoint - wallNormal * (currentRadius + 0.1f);
        hangPosition.y = ledgePoint.y - currentHeight + ledgeGrabHeight;
        transform.position = hangPosition;
        
        // Mira hacia la pared
        transform.rotation = Quaternion.LookRotation(-wallNormal);
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = true,
            LedgePosition = ledgePosition
        });
    }

    private void HandleLedgeGrab()
    {
        verticalVelocity = 0f;
        
        // Saltar del ledge
        if (jumpInputHeld && !wasJumpPressed)
        {
            LedgeJump();
            return;
        }
        
        // Bajar del ledge
        if (crouchInputHeld && !wasCrouchPressed)
        {
            ReleaseLedge();
            return;
        }
        
        // Subir al ledge
        if (InputDirection.y > 0.5f)
        {
            StartLedgeClimb();
        }
    }

    private void LedgeJump()
    {
        isGrabbingLedge = false;
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.LedgeJump;
        
        verticalVelocity = ledgeJumpForce;
        jumpMomentum = -transform.forward * ledgeJumpHorizontalForce;
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = false,
            LedgePosition = ledgePosition
        });
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.LedgeJump,
            JumpCount = jumpCount,
            JumpDirection = -transform.forward
        });
    }

    private void ReleaseLedge()
    {
        isGrabbingLedge = false;
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = false,
            LedgePosition = ledgePosition
        });
    }

    private void StartLedgeClimb()
    {
        isGrabbingLedge = false;
        isClimbingLedge = true;
        ledgeClimbTimer = 0f;
        ledgeClimbStartPos = transform.position;
        ledgeClimbTarget = ledgePosition + transform.forward * (currentRadius + 0.2f);
        ledgeClimbTarget.y = ledgePosition.y + 0.1f;
        
        EventBus.Raise(new OnPlayerLedgeGrabEvent
        {
            Player = gameObject,
            IsGrabbing = false,
            LedgePosition = ledgePosition
        });
    }

    private void HandleLedgeClimb()
    {
        ledgeClimbTimer += Time.deltaTime;
        float t = ledgeClimbTimer / ledgeClimbDuration;
        
        if (t >= 1f)
        {
            transform.position = ledgeClimbTarget;
            isClimbingLedge = false;
            isGrounded = true;
            jumpCount = 0;
            return;
        }
        
        // Curva de movimiento: primero sube, luego adelante
        float upT = Mathf.Clamp01(t * 2f);
        float forwardT = Mathf.Clamp01((t - 0.3f) / 0.7f);
        
        Vector3 upPos = ledgeClimbStartPos + Vector3.up * (ledgeClimbTarget.y - ledgeClimbStartPos.y) * upT;
        Vector3 finalPos = Vector3.Lerp(upPos, ledgeClimbTarget, forwardT);
        
        transform.position = finalPos;
    }

    private bool CastGroundRay(Vector3 origin, out RaycastHit hit)
    {
        return Physics.Raycast(origin, Vector3.down, out hit, rayLength, groundLayer);
    }

    private void UpdateTimers()
    {
        // Coyote time
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
            lastGroundedTime = Time.time;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }
        
        // Jump buffer
        if (jumpInputHeld && !wasJumpPressed)
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
        
        // Triple jump window
        if (jumpCount > 0 && isGrounded && Time.time - lastJumpTime > tripleJumpWindow)
        {
            jumpCount = 0;
        }
    }

    private void UpdateSlopeState()
    {
        isOnSteepSlope = isGrounded && currentSlopeAngle > maxWalkableAngle;
        
        if (isOnSteepSlope)
        {
            steepSlopeTimer += Time.deltaTime;
        }
        else if (!hasSlideMomentum)
        {
            steepSlopeTimer = 0f;
            slideInfluence = 0f;
        }
        
        if (steepSlopeTimer > slideActivationDelay)
        {
            float rampProgress = (steepSlopeTimer - slideActivationDelay) / slideActivationRampTime;
            slideInfluence = Mathf.Clamp01(rampProgress);
        }
        else if (!hasSlideMomentum)
        {
            slideInfluence = 0f;
        }
    }

    private void UpdateSlideMomentum()
    {
        if (!isOnSteepSlope && currentSlideSpeed > slideMinSpeedThreshold && slideInfluence > 0.5f)
        {
            if (!hasSlideMomentum)
            {
                hasSlideMomentum = true;
                slideMomentumDirection = slideVelocity.normalized;
            }
        }
        
        if (hasSlideMomentum)
        {
            bool shouldCancel = false;
            
            if (currentSlideSpeed < slideMinSpeedThreshold)
            {
                shouldCancel = true;
            }
            
            if (currentSpeed > 0.1f && moveDirection.magnitude > 0.1f)
            {
                float dot = Vector3.Dot(slideMomentumDirection, moveDirection.normalized);
                if (dot < -slideCancelThreshold)
                {
                    shouldCancel = true;
                }
            }
            
            if (isOnSteepSlope)
            {
                shouldCancel = true;
            }
            
            if (shouldCancel)
            {
                CancelSlideMomentum();
            }
        }
    }

    private void CancelSlideMomentum()
    {
        hasSlideMomentum = false;
        slideMomentumDirection = Vector3.zero;
        steepSlopeTimer = 0f;
        slideInfluence = 0f;
    }

    private void HandleCrouchLock()
    {
        if ((isCrouchSliding || (hasSlideMomentum && crouching)) && !crouchLocked)
        {
            crouchLocked = true;
        }
        
        if (crouchLocked && isGrounded && !isOnSteepSlope && !hasSlideMomentum)
        {
            crouchLocked = false;
        }
        
        crouching = crouchInputHeld || crouchLocked;
    }

    private void HandleCrouch()
    {
        float targetHeight = crouching ? crouchHeight : normalHeight;
        float targetRadius = crouching ? crouchRadius : normalRadius;
        
        currentHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        currentRadius = Mathf.Lerp(currentRadius, targetRadius, crouchTransitionSpeed * Time.deltaTime);
        
        if (Mathf.Abs(currentHeight - targetHeight) < 0.01f)
        {
            currentHeight = targetHeight;
        }
        if (Mathf.Abs(currentRadius - targetRadius) < 0.01f)
        {
            currentRadius = targetRadius;
        }
        
        controller.height = currentHeight;
        controller.radius = currentRadius;
        
        float centerOffset = (currentHeight - normalHeight) * 0.5f;
        
        if (Mathf.Abs(centerOffset) < 0.001f)
        {
            centerOffset = 0f;
        }
        
        controller.center = new Vector3(0f, centerOffset, 0f);
    }

    private void HandleMovement()
    {
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();
        
        Vector3 inputDir = (cameraForward * InputDirection.y + cameraRight * InputDirection.x).normalized;
        
        float targetSpeedMove = walkSpeed;
        if (crouching)
            targetSpeedMove = crouchSpeed;
        else if (running)
            targetSpeedMove = runSpeed;
        
        if (inputDir.magnitude > 0.1f)
        {
            float accel = isGrounded ? acceleration : acceleration * airControl;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeedMove, accel * Time.deltaTime);
            
            if (!isCrouchSliding && !isSliding && !hasSlideMomentum && isGrounded)
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
            float decel = isGrounded ? deceleration : deceleration * airControl * 0.5f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);
        }
    }

    private void HandleJump()
    {
        if (isGroundPounding || isGroundPoundStarting) return;
        
        bool jumpPressed = jumpInputHeld && !wasJumpPressed;
        bool canJump = (isGrounded || coyoteTimer > 0f) && !isJumping;
        bool hasBufferedJump = jumpBufferTimer > 0f;
        
        // Jump cut - soltar el botón reduce la altura
        if (!jumpInputHeld && isJumping && verticalVelocity > 0)
        {
            verticalVelocity *= jumpCutMultiplier;
        }
        
        if ((jumpPressed || hasBufferedJump) && canJump)
        {
            jumpBufferTimer = 0f;
            
            // Determina tipo de salto
            if (canGroundPoundJump)
            {
                PerformGroundPoundJump();
            }
            else if (crouching && totalSpeed > tripleJumpSpeedRequirement * 0.5f)
            {
                // Long jump o backflip
                float dot = Vector3.Dot(moveDirection.normalized, transform.forward);
                
                if (dot < -0.5f || InputDirection.y < -0.5f)
                {
                    PerformBackflip();
                }
                else if (totalSpeed > tripleJumpSpeedRequirement)
                {
                    PerformLongJump();
                }
                else
                {
                    PerformNormalJump();
                }
            }
            else if (isOnSteepSlope || hasSlideMomentum)
            {
                PerformSlopeJump();
            }
            else if (jumpCount == 1 && totalSpeed > tripleJumpSpeedRequirement && Time.time - lastJumpTime < tripleJumpWindow)
            {
                PerformDoubleJump();
            }
            else if (jumpCount == 2 && totalSpeed > tripleJumpSpeedRequirement && Time.time - lastJumpTime < tripleJumpWindow)
            {
                PerformTripleJump();
            }
            else
            {
                PerformNormalJump();
            }
        }
    }

    private void PerformNormalJump()
    {
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.Normal;
        lastJumpTime = Time.time;
        coyoteTimer = 0f;
        
        verticalVelocity = jumpForce;
        jumpMomentum = GetHorizontalVelocity();
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.Normal,
            JumpCount = jumpCount,
            JumpDirection = Vector3.up
        });
    }

    private void PerformDoubleJump()
    {
        isJumping = true;
        jumpCount = 2;
        lastJumpType = JumpType.Double;
        lastJumpTime = Time.time;
        
        verticalVelocity = doubleJumpForce;
        jumpMomentum = GetHorizontalVelocity();
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.Double,
            JumpCount = jumpCount,
            JumpDirection = Vector3.up
        });
    }

    private void PerformTripleJump()
    {
        isJumping = true;
        jumpCount = 3;
        lastJumpType = JumpType.Triple;
        lastJumpTime = Time.time;
        
        verticalVelocity = tripleJumpForce;
        jumpMomentum = GetHorizontalVelocity() * 1.2f;
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.Triple,
            JumpCount = jumpCount,
            JumpDirection = Vector3.up
        });
    }

    private void PerformLongJump()
    {
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.Long;
        lastJumpTime = Time.time;
        coyoteTimer = 0f;
        crouchLocked = false;
        crouching = false;
        
        verticalVelocity = longJumpForce;
        jumpMomentum = transform.forward * longJumpHorizontalBoost;
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.Long,
            JumpCount = jumpCount,
            JumpDirection = transform.forward
        });
    }

    private void PerformBackflip()
    {
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.Backflip;
        lastJumpTime = Time.time;
        coyoteTimer = 0f;
        crouchLocked = false;
        crouching = false;
        
        verticalVelocity = backflipForce;
        jumpMomentum = -transform.forward * backflipHorizontalForce;
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.Backflip,
            JumpCount = jumpCount,
            JumpDirection = -transform.forward + Vector3.up
        });
    }

    private void PerformSlopeJump()
    {
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.SlopeJump;
        lastJumpTime = Time.time;
        coyoteTimer = 0f;
        
        Vector3 slopeJumpDir = hasSlideMomentum ? slideMomentumDirection : GetSlideDirection();
        
        verticalVelocity = jumpForce * slopeJumpForceMultiplier;
        jumpMomentum = slopeJumpDir * currentSlideSpeed + lateralVelocity;
        
        // Cancela slide momentum al saltar
        if (hasSlideMomentum)
        {
            CancelSlideMomentum();
        }
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.SlopeJump,
            JumpCount = jumpCount,
            JumpDirection = slopeJumpDir + Vector3.up
        });
    }

    private void PerformGroundPoundJump()
    {
        isJumping = true;
        jumpCount = 1;
        lastJumpType = JumpType.GroundPoundJump;
        lastJumpTime = Time.time;
        canGroundPoundJump = false;
        
        verticalVelocity = groundPoundJumpForce;
        jumpMomentum = moveDirection * currentSpeed;
        
        EventBus.Raise(new OnPlayerJumpEvent
        {
            Player = gameObject,
            JumpType = JumpType.GroundPoundJump,
            JumpCount = jumpCount,
            JumpDirection = Vector3.up
        });
    }

    private void HandleGroundPound()
    {
        // Iniciar ground pound en el aire
        if (!isGrounded && crouchInputHeld && !wasCrouchPressed && !isGroundPounding && !isGroundPoundStarting)
        {
            if (verticalVelocity < jumpForce * 0.5f) // No al inicio del salto
            {
                StartGroundPound();
            }
        }
        
        // Delay antes de caer
        if (isGroundPoundStarting)
        {
            groundPoundTimer += Time.deltaTime;
            verticalVelocity = 0f;
            jumpMomentum = Vector3.zero;
            
            if (groundPoundTimer >= groundPoundDelay)
            {
                isGroundPoundStarting = false;
                isGroundPounding = true;
            }
        }
    }

    private void StartGroundPound()
    {
        isGroundPoundStarting = true;
        isGroundPounding = false;
        groundPoundTimer = 0f;
        jumpMomentum = Vector3.zero;
        currentSlideSpeed = 0f;
        
        EventBus.Raise(new OnPlayerGroundPoundEvent
        {
            Player = gameObject,
            IsStarting = true,
            IsLanding = false
        });
    }

    private Vector3 GetHorizontalVelocity()
    {
        if (hasSlideMomentum)
        {
            return slideMomentumDirection * currentSlideSpeed + lateralVelocity;
        }
        else if (isSliding)
        {
            return slideVelocity + lateralVelocity;
        }
        else
        {
            return moveDirection * currentSpeed + currentMomentum * momentumInfluence;
        }
    }

    private void HandleMomentum()
    {
        if (!isGrounded) return;
        
        if (!isOnSteepSlope && !hasSlideMomentum)
        {
            if (currentMomentum.magnitude > 0.1f)
            {
                if (currentSpeed > 0.1f && moveDirection.magnitude > 0.1f)
                {
                    Vector3 momentumDir = currentMomentum.normalized;
                    Vector3 moveDir = moveDirection.normalized;
                    float dot = Vector3.Dot(momentumDir, moveDir);
                    
                    if (dot < -0.3f)
                    {
                        float cancelSpeed = momentumDecay * 5f * Mathf.Abs(dot);
                        currentMomentum = Vector3.MoveTowards(currentMomentum, Vector3.zero, cancelSpeed * Time.deltaTime);
                    }
                    else if (dot < 0.3f)
                    {
                        currentMomentum = Vector3.MoveTowards(currentMomentum, Vector3.zero, momentumDecay * 2f * Time.deltaTime);
                    }
                    else
                    {
                        currentMomentum = Vector3.MoveTowards(currentMomentum, Vector3.zero, momentumDecay * Time.deltaTime);
                    }
                }
                else
                {
                    currentMomentum = Vector3.MoveTowards(currentMomentum, Vector3.zero, momentumDecay * Time.deltaTime);
                }
            }
        }
    }

    private float CalculateGroundStickForce(float speed)
    {
        float speedFactor = Mathf.Clamp01(speed / groundStickSpeedReference);
        float stickForce = Mathf.Lerp(groundStickForceMin, groundStickForceMax, speedFactor * speedFactor);
        return stickForce;
    }

    private void ApplyGravity()
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
        
        if (isGrounded && !isJumping)
        {
            if (controller.isGrounded)
            {
                verticalVelocity = -currentGroundStickForce;
            }
            else
            {
                verticalVelocity = -currentGroundStickForce * 2f;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }

    private Vector3 GetSlideDirection()
    {
        return Vector3.ProjectOnPlane(Vector3.down, smoothedSlopeNormal).normalized;
    }

    private float GetUphillFactor(Vector3 movementDirection)
    {
        if (currentSlopeAngle < 1f) return 0f;
        
        Vector3 projectedMovement = Vector3.ProjectOnPlane(movementDirection, smoothedSlopeNormal);
        
        if (projectedMovement.y > 0.01f)
        {
            float verticalComponent = projectedMovement.y;
            float horizontalComponent = new Vector2(projectedMovement.x, projectedMovement.z).magnitude;
            float climbRatio = verticalComponent / (verticalComponent + horizontalComponent + 0.001f);
            return Mathf.Clamp01(climbRatio * 2f + 0.3f);
        }
        
        return 0f;
    }

    private float CalculateSlideAccelerationForAngle(float angle)
    {
        if (angle <= maxWalkableAngle) return 0f;
        
        float angleFactor = (angle - maxWalkableAngle) / (90f - maxWalkableAngle);
        return Mathf.Lerp(slideAccelerationMin, slideAccelerationMax, angleFactor);
    }

    private Vector3 CalculateLateralControl(Vector3 slideDirection)
    {
        if (currentSpeed < 0.1f || moveDirection.magnitude < 0.1f)
        {
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, slideLateralAcceleration * Time.deltaTime);
            return lateralVelocity;
        }
        
        Vector3 lateralDirection;
        
        if (hasSlideMomentum && !isOnSteepSlope)
        {
            lateralDirection = moveDirection - Vector3.Project(moveDirection, slideMomentumDirection);
        }
        else
        {
            Vector3 slopeMovement = Vector3.ProjectOnPlane(moveDirection, smoothedSlopeNormal);
            lateralDirection = slopeMovement - Vector3.Project(slopeMovement, slideDirection);
        }
        
        if (lateralDirection.magnitude > 0.1f)
        {
            lateralDirection.Normalize();
            Vector3 targetLateral = lateralDirection * currentSpeed * slideControlFactor;
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, targetLateral, slideLateralAcceleration * Time.deltaTime);
        }
        else
        {
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, slideLateralAcceleration * Time.deltaTime);
        }
        
        return lateralVelocity;
    }

    private void ApplyFinalMovement()
    {
        Vector3 velocity;
        
        if (isGroundPounding || isGroundPoundStarting)
        {
            velocity = Vector3.zero;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
            return;
        }
        
        if (!isGrounded)
        {
            // En el aire
            isSliding = false;
            isCrouchSliding = false;
            struggleMeter = 1f;
            
            // Air control
            Vector3 airVelocity = jumpMomentum;
            
            if (currentSpeed > 0.1f && moveDirection.magnitude > 0.1f)
            {
                Vector3 airControl = moveDirection * currentSpeed * this.airControl;
                airVelocity = Vector3.Lerp(airVelocity, airControl + jumpMomentum * 0.5f, Time.deltaTime * 3f);
                jumpMomentum = airVelocity;
            }
            
            velocity = airVelocity;
            velocity.y = verticalVelocity;
            
            totalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            currentGroundStickForce = groundStickForceMin;
        }
        else if (hasSlideMomentum && !isOnSteepSlope)
        {
            // Slide momentum en suelo plano
            isSliding = true;
            isCrouchSliding = crouching;
            isJumping = false;
            
            currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideMomentumDecay * Time.deltaTime);
            
            slideVelocity = slideMomentumDirection * currentSlideSpeed;
            
            Vector3 lateral = CalculateLateralControl(slideMomentumDirection);
            
            velocity = slideVelocity + lateral;
            
            Vector3 totalHorizontal = new Vector3(velocity.x, 0f, velocity.z);
            if (totalHorizontal.magnitude > 0.5f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(totalHorizontal.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * 0.3f * Time.deltaTime);
            }
            
            totalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            currentGroundStickForce = CalculateGroundStickForce(totalSpeed);
            
            velocity.y = verticalVelocity;

            
            currentMomentum = new Vector3(velocity.x, 0f, velocity.z) * 0.5f;
        }
        else if (!isOnSteepSlope)
        {
            // Suelo normal
            isSliding = false;
            isCrouchSliding = false;
            isJumping = false;
            uphillAmount = 0f;
            struggleMeter = Mathf.Min(1f, struggleMeter + (struggleDecay * 0.5f) * Time.deltaTime);
            
            currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideAccelerationMax * 2f * Time.deltaTime);
            slideVelocity = Vector3.MoveTowards(slideVelocity, Vector3.zero, slideAccelerationMax * 2f * Time.deltaTime);
            lateralVelocity = Vector3.MoveTowards(lateralVelocity, Vector3.zero, slideLateralAcceleration * Time.deltaTime);
            
            Vector3 groundMovement;
            if (currentSlopeAngle > 1f)
            {
                Vector3 slopeMovement = Vector3.ProjectOnPlane(moveDirection, smoothedSlopeNormal).normalized;
                groundMovement = slopeMovement * currentSpeed;
            }
            else
            {
                groundMovement = moveDirection * currentSpeed;
            }
            
            velocity = groundMovement + currentMomentum * momentumInfluence;
            
            totalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            currentGroundStickForce = CalculateGroundStickForce(totalSpeed);
            
            velocity.y = verticalVelocity;

        }
        else
        {
            // Slope empinado
            isJumping = false;
            Vector3 slideDirection = GetSlideDirection();
            float angleFactor = (currentSlopeAngle - maxWalkableAngle) / (90f - maxWalkableAngle);
            
            currentSlideAcceleration = CalculateSlideAccelerationForAngle(currentSlopeAngle);
            
            bool hasInput = currentSpeed > 0.1f && moveDirection.magnitude > 0.1f;
            float uphillFactor = hasInput ? GetUphillFactor(moveDirection) : 0f;
            uphillAmount = uphillFactor;
            
            if (crouching)
            {
                currentSlideAcceleration *= crouchSlideBoost;
                isCrouchSliding = true;
                slideInfluence = 1f;
                steepSlopeTimer = slideActivationDelay + slideActivationRampTime;
            }
            else
            {
                isCrouchSliding = false;
            }
            
            Vector3 normalMovement = Vector3.zero;
            if (hasInput)
            {
                Vector3 slopeMovement = Vector3.ProjectOnPlane(moveDirection, smoothedSlopeNormal).normalized;
                normalMovement = slopeMovement * currentSpeed;
            }
            
            Vector3 slideMovement;
            
            if (slideInfluence > 0f)
            {
                if (uphillFactor > 0.1f && hasInput)
                {
                    float drainRate = struggleDecay * angleFactor * (1f + uphillFactor) * slideInfluence;
                    struggleMeter = Mathf.Max(0f, struggleMeter - drainRate * Time.deltaTime);
                }
                else
                {
                    struggleMeter = Mathf.Min(1f, struggleMeter + (struggleDecay * 0.5f) * Time.deltaTime);
                }
            }
            else
            {
                struggleMeter = 1f;
            }
            
            float currentStruggle = struggleMeter * struggleStrength;
            
            float effectiveAcceleration = currentSlideAcceleration * slideInfluence;
            
            if (hasInput && uphillFactor > 0.1f)
            {
                effectiveAcceleration *= (1f - currentStruggle * 0.8f);
                float pushback = currentSpeed * uphillFactor * slidePushbackMultiplier * angleFactor * (1f - currentStruggle) * slideInfluence;
                effectiveAcceleration += pushback;
            }
            
            currentSlideSpeed += effectiveAcceleration * Time.deltaTime;
            
            if (slideInfluence < 0.1f)
            {
                currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideAccelerationMax * Time.deltaTime);
            }
            
            slideVelocity = slideDirection * currentSlideSpeed;
            
            Vector3 lateral = CalculateLateralControl(slideDirection);
            
            bool isActuallySliding = slideInfluence > 0.5f && (struggleMeter <= 0.2f || !hasInput || uphillFactor <= 0.1f);
            
            if (isActuallySliding)
            {
                isSliding = true;
                slideMovement = slideVelocity + lateral;
                
                Vector3 totalHorizontal = new Vector3(slideMovement.x, 0f, slideMovement.z);
                if (totalHorizontal.magnitude > 0.5f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(totalHorizontal.normalized, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * 0.3f * Time.deltaTime);
                }
            }
            else
            {
                isSliding = false;
                
                float fightStrength = hasInput ? currentStruggle * (1f - slideInfluence * 0.5f) : 0f;
                slideMovement = slideVelocity * (1f - fightStrength * 0.5f) + lateral;
            }
            
            float normalInfluence = 1f - slideInfluence;
            velocity = normalMovement * normalInfluence + slideMovement * slideInfluence;
            
            if (slideInfluence > 0.5f && hasInput && struggleMeter > 0.2f && uphillFactor > 0.1f)
            {
                float fightBonus = currentStruggle * (1f - angleFactor * 0.5f);
                velocity += normalMovement * fightBonus * 0.5f;
            }
            
            totalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            currentGroundStickForce = CalculateGroundStickForce(totalSpeed);
            
            velocity.y = verticalVelocity;
            
            currentMomentum = new Vector3(velocity.x, 0f, velocity.z) * 0.5f;
        }
        
        controller.Move(velocity * Time.deltaTime);
        
        wasSliding = isSliding;
        wasGrounded = isGrounded;
    }

    private void RaiseEvents()
    {
        if (isGrounded != wasGrounded)
        {
            EventBus.Raise(new OnPlayerGroundedEvent
            {
                Player = gameObject,
                IsGrounded = isGrounded
            });
        }
        
        if (isSliding != wasSliding)
        {
            EventBus.Raise(new OnPlayerSlidingEvent
            {
                Player = gameObject,
                IsSliding = isSliding,
                SlideSpeed = currentSlideSpeed,
                SlideDirection = hasSlideMomentum ? slideMomentumDirection : GetSlideDirection()
            });
        }
        
        if (isCrouchSliding != wasCrouchSliding)
        {
            EventBus.Raise(new OnPlayerCrouchSlidingEvent
            {
                Player = gameObject,
                IsCrouchSliding = isCrouchSliding
            });
            wasCrouchSliding = isCrouchSliding;
        }
        
        if (crouchLocked != wasCrouchLocked)
        {
            EventBus.Raise(new OnPlayerCrouchLockedEvent
            {
                Player = gameObject,
                IsLocked = crouchLocked
            });
            wasCrouchLocked = crouchLocked;
        }
        
        if (Mathf.Abs(currentSlopeAngle - lastSlopeAngle) > 2f)
        {
            EventBus.Raise(new OnPlayerSlopeEvent
            {
                Player = gameObject,
                SlopeAngle = currentSlopeAngle,
                IsOnSteepSlope = isOnSteepSlope
            });
            lastSlopeAngle = currentSlopeAngle;
        }
    }

    // Public getters
    public bool IsGrounded() => isGrounded;
    public bool IsSliding() => isSliding;
    public bool IsCrouchSliding() => isCrouchSliding;
    public bool IsCrouchLocked() => crouchLocked;
    public bool IsOnSteepSlope() => isOnSteepSlope;
    public bool HasSlideMomentum() => hasSlideMomentum;
    public bool IsJumping() => isJumping;
    public bool IsGroundPounding() => isGroundPounding;
    public bool IsGrabbingLedge() => isGrabbingLedge;
    public int GetJumpCount() => jumpCount;
    public JumpType GetLastJumpType() => lastJumpType;
    public float GetSlideInfluence() => slideInfluence;
    public float GetCurrentSpeed() => currentSpeed;
    public float GetSlideSpeed() => currentSlideSpeed;
    public float GetTotalSpeed() => totalSpeed;
    public float GetSlopeAngle() => currentSlopeAngle;
    public Vector3 GetMomentum() => currentMomentum;
    public Vector3 GetJumpMomentum() => jumpMomentum;
    public Vector3 GetSlideMomentumDirection() => slideMomentumDirection;

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Ground rays
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * rayLength);
        
        if (controller != null)
        {
            float offset = controller.radius * 0.5f;
            Gizmos.color = Color.gray;
            Gizmos.DrawLine(rayOrigin + Vector3.forward * offset, rayOrigin + Vector3.forward * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.back * offset, rayOrigin + Vector3.back * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.left * offset, rayOrigin + Vector3.left * offset + Vector3.down * rayLength);
            Gizmos.DrawLine(rayOrigin + Vector3.right * offset, rayOrigin + Vector3.right * offset + Vector3.down * rayLength);
            
            // Collider
            if (crouchLocked)
                Gizmos.color = Color.red;
            else if (crouching)
                Gizmos.color = Color.yellow;
            else
                Gizmos.color = Color.white;
                
            Vector3 bottom = transform.position + controller.center + Vector3.down * (controller.height * 0.5f - controller.radius);
            Vector3 top = transform.position + controller.center + Vector3.up * (controller.height * 0.5f - controller.radius);
            Gizmos.DrawWireSphere(bottom, controller.radius);
            Gizmos.DrawWireSphere(top, controller.radius);
        }
        
        // Ledge grab detection
        if (!isGrounded && !isGrabbingLedge)
        {
            Gizmos.color = Color.cyan;
            Vector3 checkOrigin = transform.position + Vector3.up * (currentHeight - ledgeGrabHeight);
            Gizmos.DrawLine(checkOrigin, checkOrigin + transform.forward * ledgeGrabDistance);
        }
        
        // Ledge position
        if (isGrabbingLedge)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(ledgePosition, 0.2f);
        }
        
        // Jump momentum
        if (!isGrounded && jumpMomentum.magnitude > 0.1f)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + jumpMomentum * 0.2f);
        }
        
        // Ground pound indicator
        if (isGroundPounding || isGroundPoundStarting)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.down * 0.5f, 0.5f);
        }
        
        // Coyote time indicator
        if (!isGrounded && coyoteTimer > 0)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, coyoteTimer / coyoteTime);
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
        
        // Jump count indicator
        if (jumpCount > 0)
        {
            Gizmos.color = jumpCount == 1 ? Color.white : (jumpCount == 2 ? Color.yellow : Color.cyan);
            for (int i = 0; i < jumpCount; i++)
            {
                Gizmos.DrawWireSphere(transform.position + Vector3.up * (2.5f + i * 0.3f), 0.1f);
            }
        }
        
        if (isGrounded)
        {
            Gizmos.color = Color.cyan;
            Vector3 groundPoint = transform.position;
            Gizmos.DrawLine(groundPoint, groundPoint + smoothedSlopeNormal * 2f);
            
            float stickNormalized = (currentGroundStickForce - groundStickForceMin) / (groundStickForceMax - groundStickForceMin);
            Gizmos.color = Color.Lerp(Color.green, Color.red, stickNormalized);
            Gizmos.DrawLine(groundPoint, groundPoint + Vector3.down * (currentGroundStickForce * 0.1f));
            
            if (hasSlideMomentum)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(groundPoint + Vector3.up * 0.7f, groundPoint + Vector3.up * 0.7f + slideMomentumDirection * (currentSlideSpeed * 0.1f));
                Gizmos.DrawWireSphere(groundPoint + Vector3.up * 0.7f + slideMomentumDirection * (currentSlideSpeed * 0.1f), 0.15f);
            }
            
            if (currentSlopeAngle > 1f)
            {
                Gizmos.color = Color.Lerp(Color.yellow, Color.red, slideInfluence);
                Vector3 slideDir = GetSlideDirection();
                Gizmos.DrawLine(groundPoint + Vector3.up * 0.5f, groundPoint + Vector3.up * 0.5f + slideDir * 2f);
                
                if (lateralVelocity.magnitude > 0.1f)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawLine(groundPoint + Vector3.up * 0.5f, groundPoint + Vector3.up * 0.5f + lateralVelocity);
                }
            }
            
            if (isOnSteepSlope || hasSlideMomentum)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, slideInfluence);
                Vector3 barStart = transform.position + Vector3.up * 2.5f + Vector3.left * 0.3f;
                Vector3 barEnd = barStart + Vector3.right * 0.6f * slideInfluence;
                Gizmos.DrawLine(barStart, barEnd);
                Gizmos.DrawWireCube(barStart + Vector3.right * 0.3f, new Vector3(0.6f, 0.1f, 0.1f));
                
                Gizmos.color = Color.Lerp(Color.red, Color.green, struggleMeter);
                Gizmos.DrawWireSphere(transform.position + Vector3.up * 2f, 0.1f + struggleMeter * 0.3f);
            }
        }
    }
}