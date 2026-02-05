using UnityEngine;

/// <summary>
/// Controlador central de estados del jugador.
/// VERSIÓN CON DEBUG MEJORADO para diagnosticar problemas.
/// </summary>
public class PlayerStateController : MonoBehaviour
{
    [Header("Speed Thresholds")]
    [SerializeField] private float walkThreshold = 0.1f;
    [SerializeField] private float runThreshold = 4f;
    [SerializeField] private float sprintThreshold = 12f;
    [SerializeField] private float maxSpeed = 16f;
    
    [Header("Vertical Thresholds")]
    [SerializeField] private float apexThreshold = 1f;
    [SerializeField] private float maxVerticalVelocity = 20f;
    
    [Header("Landing Config")]
    [SerializeField] private float landingStateDuration = 0.2f;
    [SerializeField] private float hardLandingStateDuration = 0.5f;
    [SerializeField] private float groundPoundLandDuration = 0.6f;
    [SerializeField] private float hardLandingFallThreshold = 5f;  // NEW: Fall distance for hard landing
    
    [Header("=== DEBUG: Current State ===")]
    [SerializeField] private PlayerState currentState = PlayerState.Idle;
    [SerializeField] private PlayerState previousState = PlayerState.Idle;
    [SerializeField] private VerticalState verticalState = VerticalState.Grounded;
    [SerializeField] private PlayerMovementPhase movementPhase = PlayerMovementPhase.None;
    [SerializeField] private TurnType currentTurnType = TurnType.None;
    
    [Header("=== DEBUG: Received Data ===")]
    [SerializeField] private float currentSpeed = 0f;
    [SerializeField] private float verticalVelocity = 0f;
    [SerializeField] private Vector3 moveDirection = Vector3.zero;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    [SerializeField] private float rotationState = 1f;
    
    [Header("=== DEBUG: Flags ===")]
    [SerializeField] private bool isGrounded = true;
    [SerializeField] private bool wasGrounded = true;  // NEW: track previous frame
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool isSliding = false;
    [SerializeField] private bool isSkidding = false;
    [SerializeField] private bool isDiving = false;
    [SerializeField] private bool isGroundPounding = false;
    [SerializeField] private bool isInHangTime = false;
    [SerializeField] private float airborneStartY = 0f;  // NEW: track jump height
    
    [Header("=== DEBUG: Event Reception ===")]
    [SerializeField] private int moveEventsReceived = 0;
    [SerializeField] private int groundedEventsReceived = 0;
    [SerializeField] private int stateChangesEmitted = 0;
    [SerializeField] private string lastEventReceived = "None";
    [SerializeField] private bool playerControllerFound = false;
    
    // Private cached data
    private bool isBeingPushedDown = false;
    private bool completedGroundPound = false;
    private float slopeAngle = 0f;
    private Vector3 skidDirection = Vector3.zero;
    private JumpType? lastJumpType = null;
    private float stateEnterTime = 0f;
    private float landingEndTime = -1f;
    
    private float lastSpeed = 0f;
    private Vector3 lastMoveDirection = Vector3.zero;
    
    private PlayerController playerController;
    
    public PlayerState CurrentState => currentState;
    public VerticalState CurrentVerticalState => verticalState;
    public bool IsSkidding => isSkidding;
    
    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        playerControllerFound = playerController != null;
        
        if (!playerControllerFound)
        {
            Debug.LogError("[PlayerStateController] PlayerController NOT FOUND! Make sure this script is on the same GameObject as PlayerController.");
        }
        else
        {
            Debug.Log("[PlayerStateController] PlayerController found successfully.");
        }
    }
    
    void OnEnable()
    {
        Debug.Log("[PlayerStateController] Subscribing to events...");
        
        EventBus.Subscribe<OnMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<OnCrouchInputEvent>(OnCrouchInput);
        EventBus.Subscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Subscribe<OnPlayerStopEvent>(OnPlayerStop);
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnPlayerLandEvent>(OnPlayerLand);
        EventBus.Subscribe<OnExecuteJumpCommand>(OnJumpExecuted);
        EventBus.Subscribe<OnSetHangTimeState>(OnHangTimeState);
        EventBus.Subscribe<OnPlayerGroundPoundEvent>(OnGroundPound);
        EventBus.Subscribe<OnPlayerDiveEvent>(OnDive);
        EventBus.Subscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Subscribe<OnPlayerStopSlidingEvent>(OnStopSliding);
        EventBus.Subscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Subscribe<OnPlayerSkidEvent>(OnSkid);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnMoveInputEvent>(OnMoveInput);
        EventBus.Unsubscribe<OnCrouchInputEvent>(OnCrouchInput);
        EventBus.Unsubscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Unsubscribe<OnPlayerStopEvent>(OnPlayerStop);
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Unsubscribe<OnPlayerLandEvent>(OnPlayerLand);
        EventBus.Unsubscribe<OnExecuteJumpCommand>(OnJumpExecuted);
        EventBus.Unsubscribe<OnSetHangTimeState>(OnHangTimeState);
        EventBus.Unsubscribe<OnPlayerGroundPoundEvent>(OnGroundPound);
        EventBus.Unsubscribe<OnPlayerDiveEvent>(OnDive);
        EventBus.Unsubscribe<OnPlayerSlideStateEvent>(OnSlideState);
        EventBus.Unsubscribe<OnPlayerStopSlidingEvent>(OnStopSliding);
        EventBus.Unsubscribe<OnPlayerSlopeEvent>(OnSlope);
        EventBus.Unsubscribe<OnPlayerSkidEvent>(OnSkid);
    }
    
    void Update()
    {
        UpdateVerticalState();
        UpdateMovementPhase();
        UpdateTurnType();
        DetermineState();
        SendAnimationData();
    }
    
    // ========================================================================
    // EVENT HANDLERS
    // ========================================================================
    
    private void OnMoveInput(OnMoveInputEvent ev)
    {
        inputDirection = ev.Direction;
        lastEventReceived = "OnMoveInputEvent";
    }
    
    private void OnCrouchInput(OnCrouchInputEvent ev)
    {
        isCrouching = ev.pressed;
        lastEventReceived = "OnCrouchInputEvent";
    }
    
    private void OnPlayerMove(OnPlayerMoveEvent ev)
    {
        lastSpeed = currentSpeed;
        lastMoveDirection = moveDirection;
        
        currentSpeed = ev.speed;
        moveDirection = new Vector3(ev.Direction.x, 0f, ev.Direction.y);
        rotationState = ev.rotationState;
        
        moveEventsReceived++;
        lastEventReceived = $"OnPlayerMoveEvent (spd:{ev.speed:F1})";
    }
    
    private void OnPlayerStop(OnPlayerStopEvent ev)
    {
        currentSpeed = 0f;
        lastEventReceived = "OnPlayerStopEvent";
    }
    
    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        wasGrounded = isGrounded;
        isGrounded = true;
        groundedEventsReceived++;
        lastEventReceived = "OnPlayerGroundedEvent";
        
        // NEW: Detect normal landing (was in air, now grounded)
        if (!wasGrounded && !isGroundPounding)
        {
            // Calculate fall distance for hard landing detection
            float fallDistance = airborneStartY - transform.position.y;
            bool isHardLanding = fallDistance > hardLandingFallThreshold;
            
            if (isDiving)
            {
                // Dive landing
                SetState(PlayerState.HardLanding);
                landingEndTime = Time.time + hardLandingStateDuration;
                isDiving = false;
            }
            else if (isHardLanding)
            {
                SetState(PlayerState.HardLanding);
                landingEndTime = Time.time + hardLandingStateDuration;
            }
            else
            {
                SetState(PlayerState.Landing);
                landingEndTime = Time.time + landingStateDuration;
            }
        }
    }
    
    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        wasGrounded = isGrounded;
        isGrounded = false;
        lastEventReceived = "OnPlayerAirborneEvent";
        
        // NEW: Track starting height for fall distance calculation
        airborneStartY = ev.YHeight;
        
        if (isSkidding) isSkidding = false;
    }
    
    private void OnPlayerLand(OnPlayerLandEvent ev)
    {
        lastEventReceived = "OnPlayerLandEvent";
        
        // This event is specifically for ground pound landing
        // Normal landings are handled in OnGrounded
        if (ev.FromGroundPound)
        {
            isGroundPounding = false;
            isDiving = false;
            completedGroundPound = true;
            SetState(PlayerState.GroundPoundLand);
            landingEndTime = Time.time + groundPoundLandDuration;
        }
    }
    
    private void OnJumpExecuted(OnExecuteJumpCommand cmd)
    {
        lastJumpType = cmd.JumpType.jumpType;
        completedGroundPound = false;
        isSkidding = false;
        lastEventReceived = $"OnExecuteJumpCommand ({cmd.JumpType.jumpType})";
        
        // CRÍTICO: Cancelar landing state cuando se ejecuta un salto
        // Esto previene que DetermineState() sobreescriba el estado de salto con Running/Walking
        if (IsInLandingState())
        {
            landingEndTime = -1f; // Cancelar protección de landing
        }
        
        // NUEVO: Verificar si debemos mantener la animación de triple jump
        if (cmd.KeepTripleJumpAnimation && cmd.JumpType.jumpType == JumpType.Normal)
        {
            // Ejecutar normal jump con animación de triple jump
            SetState(PlayerState.TripleJumping);
            return;
        }
        
        switch (cmd.JumpType.jumpType)
        {
            case JumpType.Normal: SetState(PlayerState.Jumping); break;
            case JumpType.Double: SetState(PlayerState.DoubleJumping); break;
            case JumpType.Triple: SetState(PlayerState.TripleJumping); break;
            case JumpType.LongJump: SetState(PlayerState.LongJump); break;
            case JumpType.Backflip: SetState(PlayerState.Backflip); break;
            case JumpType.GroundPoundJump: SetState(PlayerState.GroundPoundJump); break;
            case JumpType.GroundPound:
                isGroundPounding = true;
                SetState(PlayerState.GroundPoundStart);
                break;
            case JumpType.Dive:
                isDiving = true;
                isGroundPounding = false;  // FIX: Cancel ground pound when diving
                isInHangTime = false;      // FIX: Also cancel hang time
                SetState(PlayerState.Diving);
                break;
            case JumpType.GroundDive:
                isDiving = true;
                SetState(PlayerState.GroundDiving);
                break;
        }
    }
    
    private void OnHangTimeState(OnSetHangTimeState state)
    {
        isInHangTime = state.IsInHangTime;
        lastEventReceived = $"OnSetHangTimeState ({state.IsInHangTime})";
        if (isInHangTime) SetState(PlayerState.HangTime);
    }
    
    private void OnGroundPound(OnPlayerGroundPoundEvent ev)
    {
        isGroundPounding = true;
        lastEventReceived = "OnPlayerGroundPoundEvent";
        SetState(PlayerState.GroundPoundFall);
    }
    
    private void OnDive(OnPlayerDiveEvent ev)
    {
        isDiving = true;
        isGroundPounding = false;  // FIX: Cancel ground pound when diving
        isInHangTime = false;      // FIX: Also cancel hang time
        lastEventReceived = "OnPlayerDiveEvent";
    }
    
    private void OnSlideState(OnPlayerSlideStateEvent ev)
    {
        isSliding = ev.IsSliding;
        isBeingPushedDown = ev.IsBeingPushedDown;
        lastEventReceived = $"OnPlayerSlideStateEvent ({ev.IsSliding})";
    }
    
    private void OnStopSliding(OnPlayerStopSlidingEvent ev)
    {
        isSliding = false;
        isBeingPushedDown = false;
        lastEventReceived = "OnPlayerStopSlidingEvent";
    }
    
    private void OnSlope(OnPlayerSlopeEvent ev)
    {
        slopeAngle = ev.SlopeAngle;
    }
    
    private void OnSkid(OnPlayerSkidEvent ev)
    {
        isSkidding = ev.IsSkidding;
        skidDirection = ev.SkidDirection;
        lastEventReceived = $"OnPlayerSkidEvent ({ev.IsSkidding})";
        if (isSkidding) SetState(PlayerState.Skidding);
    }
    
    // ========================================================================
    // STATE DETERMINATION
    // ========================================================================
    
    private void UpdateVerticalState()
    {
        if (playerController != null)
        {
            verticalVelocity = playerController.verticalVelocity;
        }
        
        if (isGrounded)
            verticalState = VerticalState.Grounded;
        else if (Mathf.Abs(verticalVelocity) < apexThreshold)
            verticalState = VerticalState.Apex;
        else if (verticalVelocity > 0)
            verticalState = VerticalState.Rising;
        else
            verticalState = VerticalState.Falling;
    }
    
    private void UpdateMovementPhase()
    {
        if (isSkidding)
        {
            movementPhase = PlayerMovementPhase.Skidding;
            return;
        }
        
        if (currentSpeed < walkThreshold)
        {
            movementPhase = PlayerMovementPhase.None;
            return;
        }
        
        float speedDelta = currentSpeed - lastSpeed;
        float directionAngle = Vector3.Angle(moveDirection, lastMoveDirection);
        
        if (directionAngle > 90f)
            movementPhase = PlayerMovementPhase.SharpTurn;
        else if (directionAngle > 30f)
            movementPhase = PlayerMovementPhase.Turning;
        else if (speedDelta > 0.1f)
            movementPhase = PlayerMovementPhase.Accelerating;
        else if (speedDelta < -0.1f)
            movementPhase = PlayerMovementPhase.Decelerating;
        else if (currentSpeed >= maxSpeed * 0.95f)
            movementPhase = PlayerMovementPhase.AtMaxSpeed;
        else
            movementPhase = PlayerMovementPhase.None;
    }
    
    private void UpdateTurnType()
    {
        if (playerController != null)
        {
            var turnState = playerController.GetCurrentTurnState();
            
            switch (turnState)
            {
                case PlayerController.TurnState.None:
                    currentTurnType = (currentSpeed < 0.5f && inputDirection.magnitude > 0.1f) 
                        ? TurnType.Instant 
                        : TurnType.None;
                    break;
                case PlayerController.TurnState.ArcTurn:
                    currentTurnType = TurnType.Arc;
                    break;
                case PlayerController.TurnState.Skidding:
                case PlayerController.TurnState.SkidTurning:
                    currentTurnType = TurnType.Skid;
                    break;
            }
        }
    }
    
    private void DetermineState()
    {
        // === PROTECTED STATES: Don't override these ===
        // Landing states have a duration - let them finish
        if (IsInLandingState() && Time.time < landingEndTime) return;
        
        // HangTime is protected
        if (isInHangTime) return;
        
        // Ground Pound sequence is protected
        if (isGroundPounding)
        {
            if (currentState != PlayerState.GroundPoundStart && 
                currentState != PlayerState.GroundPoundFall)
            {
                SetState(PlayerState.GroundPoundFall);
            }
            return;
        }
        
        // NUEVO: Proteger estados de salto recién establecidos
        // Si acabamos de entrar en un estado de salto (menos de 0.1s), no lo sobreescribir
        if (IsInAnyJumpState() && GetTimeInCurrentState() < 0.1f)
        {
            return;
        }
        
        // Diving in air is protected until landing
        if (isDiving && !isGrounded) return;
        
        // Special jump states (LongJump, Backflip) are protected while in air
        if (IsInSpecialJumpState() && !isGrounded) return;
        
        // === GROUNDED STATES ===
        if (isGrounded)
        {
            isDiving = false;
            
            // Skid priority
            if (isSkidding)
            {
                if (currentState != PlayerState.Skidding && currentState != PlayerState.SkidTurn)
                    SetState(PlayerState.Skidding);
                return;
            }
            
            // SkidTurn check
            if (playerController != null && 
                playerController.GetCurrentTurnState() == PlayerController.TurnState.SkidTurning)
            {
                SetState(PlayerState.SkidTurn);
                return;
            }
            
            // Sliding
            if (isSliding)
            {
                SetState(PlayerState.SlopeSliding);
                return;
            }
            
            // Crouching
            if (isCrouching)
            {
                SetState(currentSpeed > walkThreshold ? PlayerState.CrouchWalk : PlayerState.CrouchIdle);
                return;
            }
            
            // Normal movement states based on speed
            if (currentSpeed < walkThreshold)
                SetState(PlayerState.Idle);
            else if (currentSpeed < runThreshold)
                SetState(PlayerState.Walking);
            else if (currentSpeed < sprintThreshold)
                SetState(PlayerState.Running);
            else
                SetState(PlayerState.Sprinting);
        }
        // === AIRBORNE STATES ===
        else
        {
            // Don't override special air states with Falling
            if (IsInSpecialJumpState()) return;
            
            // CORREGIDO: Protección mejorada para saltos del combo
            if (IsInNormalJumpState())
            {
                // Proteger el estado de salto hasta que esté REALMENTE cayendo
                // Criterios para transicionar a Falling:
                // 1. Velocidad vertical significativamente negativa (cayendo rápido)
                // 2. Ha pasado suficiente tiempo desde el inicio del estado (no interrumpir animación temprano)
                
                float timeInJumpState = Time.time - stateEnterTime;
                bool fallingLongEnough = verticalVelocity < -8f;  // Threshold más razonable
                bool enoughTimeInState = timeInJumpState > 0.3f;  // Dar tiempo a la animación
                
                // Solo transicionar si está cayendo Y ha pasado tiempo suficiente
                if (fallingLongEnough && enoughTimeInState)
                {
                    SetState(PlayerState.Falling);
                }
                return;
            }
            
            // If not in any jump state and in air, set to falling
            if (!IsInAnyJumpState())
            {
                SetState(PlayerState.Falling);
            }
        }
    }
    
    private void SetState(PlayerState newState)
    {
        if (currentState == newState) return;
        
        previousState = currentState;
        currentState = newState;
        stateEnterTime = Time.time;
        stateChangesEmitted++;
        
        EventBus.Raise(new OnPlayerStateChangedEvent()
        {
            Player = gameObject,
            CurrentState = currentState,
            PreviousState = previousState,
            Speed = currentSpeed,
            NormalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed),
            MoveDirection = moveDirection,
            RotationState = rotationState,
            VerticalState = verticalState,
            VerticalVelocity = verticalVelocity,
            NormalizedVerticalVelocity = Mathf.Clamp(verticalVelocity / maxVerticalVelocity, -1f, 1f),
            MovementPhase = movementPhase,
            CurrentTurnType = currentTurnType,
            LastJumpType = lastJumpType,
            IsCrouching = isCrouching,
            IsSliding = isSliding,
            IsSkidding = isSkidding,
            SlopeAngle = slopeAngle,
            StateEnterTime = stateEnterTime,
            TimeSinceStateChange = 0f
        });
    }
    
    private void SendAnimationData()
    {
        EventBus.Raise(new OnPlayerAnimationDataEvent()
        {
            Player = gameObject,
            Speed = currentSpeed,
            NormalizedSpeed = Mathf.Clamp01(currentSpeed / maxSpeed),
            VerticalVelocity = verticalVelocity,
            NormalizedVerticalVelocity = Mathf.Clamp(verticalVelocity / maxVerticalVelocity, -1f, 1f),
            RotationState = rotationState,
            InputDirection = inputDirection,
            IsMoving = currentSpeed > walkThreshold,
            IsGrounded = isGrounded,
            IsSkidding = isSkidding,
            CurrentTurnType = currentTurnType
        });
    }
    
    // ========================================================================
    // HELPER METHODS
    // ========================================================================
    
    private bool IsInNormalJumpState()
    {
        return currentState == PlayerState.Jumping ||
               currentState == PlayerState.DoubleJumping ||
               currentState == PlayerState.TripleJumping;
    }
    
    private bool IsInSpecialJumpState()
    {
        return currentState == PlayerState.LongJump ||
               currentState == PlayerState.Backflip ||
               currentState == PlayerState.GroundPoundJump ||
               currentState == PlayerState.Diving ||
               currentState == PlayerState.GroundDiving;
    }
    
    private bool IsInAnyJumpState()
    {
        return IsInNormalJumpState() || IsInSpecialJumpState();
    }
    
    private bool IsInJumpState()
    {
        return IsInAnyJumpState();
    }
    
    private bool IsInLandingState()
    {
        return currentState == PlayerState.Landing ||
               currentState == PlayerState.HardLanding ||
               currentState == PlayerState.GroundPoundLand ||
               currentState == PlayerState.DiveLand;
    }
    
    // ========================================================================
    // PUBLIC METHODS
    // ========================================================================
    
    public bool IsAirborne() => !isGrounded;
    public bool IsPerformingAction() => isDiving || isGroundPounding || isInHangTime;
    public float GetTimeInCurrentState() => Time.time - stateEnterTime;
    public bool JustTransitionedFrom(PlayerState state) => previousState == state && GetTimeInCurrentState() < 0.1f;
    public TurnType GetCurrentTurnType() => currentTurnType;
}