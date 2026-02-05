using UnityEngine;

public class JumpController : MonoBehaviour
{
    [Header("JumpTypes")]
    public JumpTypeCreator normalJump;
    public JumpTypeCreator doubleJump;
    public JumpTypeCreator tripleJump;
    public JumpTypeCreator dive;
    public JumpTypeCreator backflip;
    public JumpTypeCreator longjump;
    public JumpTypeCreator groundPound;
    public JumpTypeCreator groundPoundJump;
    public JumpTypeCreator groundDive;

    [Header("Config")] 
    public float delayBetweenJumps = 0.2f;
    public float jumpChainWindow = 0.6f; // Aumentado a 0.6s para testing más fácil
    public float longjumpSpeedThreshold = 0.5f;
    public float comboSpeedThreshold = 0.3f;
    public float groundDiveSpeedThreshold = 0.3f;
    
    [Header("Longjump Config")]
    public float longjumpCrouchDelay = 0.15f;
    
    [Header("Ground Pound Config")]
    public float groundPoundJumpWindow = 0.5f;
    public float groundPoundForce = -50f;
    
    [Header("Input Detection")]
    public float backflipInputThreshold = -0.7f;
    public float backflipDirectionChangeAngle = 120f;
    
    [Header("Debug")]
    [SerializeField] private int currentJumpInChain = 0;
    [SerializeField] private float lastJumpTime = -1f;
    [SerializeField] private float lastGroundedTime = -1f;
    [SerializeField] private float lastDiveTime = -1f;
    [SerializeField] private bool isDiving = false;
    
    [SerializeField] private bool isGroundPounding = false;
    [SerializeField] private float groundPoundStartTime = -1f;
    [SerializeField] private bool canCancelGroundPound = false;
    [SerializeField] private bool completedGroundPound = false;
    [SerializeField] private float groundPoundCompleteTime = -1f;
    
    [SerializeField] private bool isInHangTime = false;
    [SerializeField] private float hangTimeStart = -1f;
    [SerializeField] private float currentHangDuration = 0f;
    [SerializeField] private JumpTypeCreator pendingJumpAfterHang = null;
    
    [SerializeField] private float currentSpeed = 0f;
    [SerializeField] private bool grounded = false;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool jumpConsumed = false; // NUEVO: Si ya se usó este input
    [SerializeField] private bool isAction = false;
    [SerializeField] private bool actionConsumed = false; // NUEVO: Si ya se usó este input
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool crouchPressedInAir = false;
    [SerializeField] private float crouchStartTime = -1f;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    
    [SerializeField] private Vector3 currentMoveDirection3D = Vector3.zero;
    [SerializeField] private Quaternion currentPlayerRotation = Quaternion.identity;
    [SerializeField] private bool recentSharpTurn = false;
    [SerializeField] private float sharpTurnTime = -1f;
    [SerializeField] private float sharpTurnAngle = 0f;
    [SerializeField] private bool backflipConsumed = false;
    [SerializeField] private bool ableToMove = false;
    
    // Skid state tracking
    [SerializeField] private bool isSkidding = false;
    [SerializeField] private Vector3 skidDirection = Vector3.zero;
    
    void OnEnable()
    {
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnJumpInputEvent>(OnJumpInput);
        EventBus.Subscribe<OnMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<OnActionInputEvent>(OnActionInput);
        EventBus.Subscribe<OnCrouchInputEvent>(OnCrouchInput);
        EventBus.Subscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Subscribe<OnPlayerLandEvent>(OnPlayerLand);
        EventBus.Subscribe<OnDirectionChangeEvent>(OnDirectionChange);
        EventBus.Subscribe<OnPlayerSkidEvent>(OnSkid);
        
        EventBus.Subscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Subscribe<onDialogueClose>(open => ableToMove = true);
    }
    
    void OnDisable()
    {
        EventBus.Unsubscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Unsubscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Unsubscribe<OnJumpInputEvent>(OnJumpInput);
        EventBus.Unsubscribe<OnMoveInputEvent>(OnMoveInput);
        EventBus.Unsubscribe<OnActionInputEvent>(OnActionInput);
        EventBus.Unsubscribe<OnCrouchInputEvent>(OnCrouchInput);
        EventBus.Unsubscribe<OnPlayerMoveEvent>(OnPlayerMove);
        EventBus.Unsubscribe<OnPlayerLandEvent>(OnPlayerLand);
        EventBus.Unsubscribe<OnDirectionChangeEvent>(OnDirectionChange);
        EventBus.Unsubscribe<OnPlayerSkidEvent>(OnSkid);
        
        EventBus.Unsubscribe<onDialogueOpen>(open => ableToMove = false);
        EventBus.Unsubscribe<onDialogueClose>(open => ableToMove = true);
    }
    
    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = ev.IsGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            isDiving = false;
            crouchPressedInAir = false;
            backflipConsumed = false;
            
            if (isGroundPounding)
            {
                EventBus.Raise<OnPlayerLandEvent>(new OnPlayerLandEvent()
                {
                    Player = gameObject,
                    YHeight = 0f,
                    HardLanding = true,
                    FromGroundPound = true
                });
                
                completedGroundPound = true;
                groundPoundCompleteTime = Time.time;
                isGroundPounding = false;
                canCancelGroundPound = false;
            }
            
            if (isInHangTime)
            {
                isInHangTime = false;
                pendingJumpAfterHang = null;
                
                EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
                {
                    Player = gameObject,
                    IsInHangTime = false
                });
            }
        }
    }
    
    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        grounded = false;
    }
    
    private void OnPlayerLand(OnPlayerLandEvent ev)
    {
        if (isInHangTime)
        {
            isInHangTime = false;
            pendingJumpAfterHang = null;
            
            EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
            {
                Player = gameObject,
                IsInHangTime = false
            });
        }
    }
    
    private void OnJumpInput(OnJumpInputEvent e)
    {
        isJumping = e.pressed;
        
        // NUEVO: Resetear consumed cuando suelta el botón
        if (!e.pressed)
        {
            jumpConsumed = false;
            backflipConsumed = false;
        }
    }
    
    private void OnActionInput(OnActionInputEvent e)
    {
        isAction = e.pressed;
        
        // NUEVO: Resetear consumed cuando suelta el botón
        if (!e.pressed)
        {
            actionConsumed = false;
        }
    }
    
    private void OnCrouchInput(OnCrouchInputEvent e)
    {
        bool wasCrouching = isCrouching;
        isCrouching = e.pressed;
        
        if (isCrouching && !wasCrouching)
        {
            if (grounded)
            {
                crouchStartTime = Time.time;
            }
            else
            {
                crouchPressedInAir = true;
            }
        }
        
        if (!isCrouching)
        {
            crouchPressedInAir = false;
            crouchStartTime = -1f;
        }
    }
    
    private void OnMoveInput(OnMoveInputEvent e)
    {
        inputDirection = e.Direction;
    }
    
    private void OnPlayerMove(OnPlayerMoveEvent e)
    {
        currentSpeed = e.speed;
        currentMoveDirection3D = new Vector3(e.Direction.x, 0, e.Direction.y);
        currentPlayerRotation = e.Rotation;
    }
    
    private void OnDirectionChange(OnDirectionChangeEvent ev)
    {
        if (ev.AngleChange > backflipDirectionChangeAngle)
        {
            recentSharpTurn = true;
            sharpTurnTime = Time.time;
            sharpTurnAngle = ev.AngleChange;
        }
    }
    
    private void OnSkid(OnPlayerSkidEvent ev)
    {
        isSkidding = ev.IsSkidding;
        skidDirection = ev.SkidDirection;
        
        // Reset backflip consumed cuando empieza un nuevo skid
        if (isSkidding)
        {
            backflipConsumed = false;
        }
    }
    
    void Update()
    {
        if(!ableToMove)return;
        
        HandleHangTime();
        HandleGroundPound();
        HandleDive();
        HandleGroundDive();
        HandleJumpLogic();
        
        // La lógica de reset del combo ahora está completamente dentro de HandleJumpLogic()
        // No necesitamos verificación adicional aquí
        
        if (completedGroundPound && Time.time > groundPoundCompleteTime + groundPoundJumpWindow)
        {
            completedGroundPound = false;
        }
        
        if (recentSharpTurn && Time.time > sharpTurnTime + 0.3f)
        {
            recentSharpTurn = false;
        }
    }
    
    private void HandleHangTime()
    {
        if (!isInHangTime) return;
        
        if (Time.time >= hangTimeStart + currentHangDuration)
        {
            isInHangTime = false;
            
            EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
            {
                Player = gameObject,
                IsInHangTime = false
            });
            
            if (pendingJumpAfterHang != null)
            {
                float force;
                if (pendingJumpAfterHang.jumpForce <= 0)
                {
                    force = groundPoundForce;
                }
                else
                {
                    force = Mathf.Sqrt(pendingJumpAfterHang.jumpForce * -2f * -9.81f);
                }
                
                EventBus.Raise<OnApplyJumpForceCommand>(new OnApplyJumpForceCommand()
                {
                    Player = gameObject,
                    Force = force
                });
                
                pendingJumpAfterHang = null;
            }
        }
    }
    
    private void HandleGroundPound()
    {
        if (crouchPressedInAir && !grounded && !isGroundPounding && !isDiving && groundPound != null)
        {
            if (RequestJump(groundPound))
            {
                isGroundPounding = true;
                groundPoundStartTime = Time.time;
                canCancelGroundPound = true;
                currentJumpInChain = 0;
                crouchPressedInAir = false;
                
                EventBus.Raise<OnPlayerGroundPoundEvent>(new OnPlayerGroundPoundEvent()
                {
                    Player = gameObject
                });
            }
        }
        
        if (isGroundPounding && canCancelGroundPound)
        {
            if (Time.time > groundPoundStartTime + groundPound.hangTime)
            {
                canCancelGroundPound = false;
            }
        }
    }
    
    private void HandleDive()
    {
        // NUEVO: Verificar que no esté consumido
        if (isAction && !actionConsumed && !grounded && !isDiving && dive != null && Time.time > lastDiveTime + delayBetweenJumps)
        {
            if (isGroundPounding && canCancelGroundPound)
            {
                if (RequestJump(dive))
                {
                    lastDiveTime = Time.time;
                    isDiving = true;
                    isGroundPounding = false;
                    canCancelGroundPound = false;
                    currentJumpInChain = 0;
                    actionConsumed = true; // NUEVO: Consumir input
                    
                    if (isInHangTime)
                    {
                        isInHangTime = false;
                        pendingJumpAfterHang = null;
                        
                        EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
                        {
                            Player = gameObject,
                            IsInHangTime = false
                        });
                    }
                    
                    EventBus.Raise<OnPlayerDiveEvent>(new OnPlayerDiveEvent()
                    {
                        Player = gameObject
                    });
                }
            }
        }
    }
    
    private void HandleGroundDive()
    {
        if(!ableToMove) return;
        
        // NUEVO: Verificar que no esté consumido
        if (isAction && !actionConsumed && grounded && !isDiving && groundDive != null && Time.time > lastDiveTime + delayBetweenJumps && !isCrouching)
        {
            if (currentSpeed >= groundDiveSpeedThreshold)
            {
                if (RequestJump(groundDive))
                {
                    lastDiveTime = Time.time;
                    isDiving = true;
                    currentJumpInChain = 0;
                    actionConsumed = true; // NUEVO: Consumir input
                    
                    EventBus.Raise<OnPlayerDiveEvent>(new OnPlayerDiveEvent()
                    {
                        Player = gameObject
                    });
                }
            }
        }
    }
    
    private void HandleJumpLogic()
    {
        if (isDiving || isGroundPounding || isInHangTime) return;
        
        // NUEVO: Verificar que no esté consumido
        if (!isJumping || jumpConsumed || Time.time < lastJumpTime + delayBetweenJumps)
            return;
        
        JumpTypeCreator jumpToExecute = null;
        
        if (grounded && completedGroundPound && groundPoundJump != null)
        {
            jumpToExecute = groundPoundJump;
            completedGroundPound = false;
            currentJumpInChain = 0;
        }
        else if (grounded && isCrouching)
        {
            bool crouchDelayPassed = crouchStartTime > 0 && Time.time >= crouchStartTime + longjumpCrouchDelay;
            
            if (crouchDelayPassed && currentSpeed > longjumpSpeedThreshold && longjump != null)
            {
                jumpToExecute = longjump;
                currentJumpInChain = 0;
            }
            else if (crouchDelayPassed && backflip != null)
            {
                jumpToExecute = backflip;
                currentJumpInChain = 0;
            }
            else
            {
                jumpToExecute = normalJump;
            }
        }
        // NUEVO: Backflip durante skid (más intuitivo que detección de cambio de dirección)
        else if (grounded && isSkidding && backflip != null && !backflipConsumed)
        {
            // Durante skid, presionar Jump ejecuta backflip
            Debug.Log("[Backflip] Ejecutando backflip desde SKID");
            jumpToExecute = backflip;
            currentJumpInChain = 0;
            backflipConsumed = true;
        }
        
        if (jumpToExecute == null && grounded)
        {
            // === NUEVA LÓGICA DE COMBO ROBUSTA ===
            
            float timeSinceLastLanding = Time.time - lastGroundedTime;
            bool hasEnoughSpeed = currentSpeed >= comboSpeedThreshold;
            bool landedRecently = timeSinceLastLanding < jumpChainWindow;
            
            // DEBUG: Log para diagnosticar
            Debug.Log($"[JumpCombo] === INICIO DE VERIFICACIÓN ===");
            Debug.Log($"[JumpCombo] currentJumpInChain: {currentJumpInChain}");
            Debug.Log($"[JumpCombo] timeSinceLastLanding: {timeSinceLastLanding:F3}s");
            Debug.Log($"[JumpCombo] landedRecently: {landedRecently} (< {jumpChainWindow}s)");
            Debug.Log($"[JumpCombo] hasEnoughSpeed: {hasEnoughSpeed} (speed: {currentSpeed:F2} >= {comboSpeedThreshold})");
            
            // IMPORTANTE: El primer salto (counter = 0) NO requiere ventana de tiempo
            // Solo los saltos del combo (counter > 0) requieren que hayas aterrizado recientemente
            if (currentJumpInChain > 0)
            {
                // En combo activo: verificar ventana y velocidad
                if (!landedRecently || !hasEnoughSpeed)
                {
                    Debug.LogWarning($"[JumpCombo] RESETEANDO contador! Razón: {(!landedRecently ? $"Tiempo desde landing ({timeSinceLastLanding:F2}s) > ventana ({jumpChainWindow}s)" : $"Velocidad ({currentSpeed:F2}) < threshold ({comboSpeedThreshold})")}");
                    currentJumpInChain = 0;
                }
            }
            else
            {
                // Primer salto: NO verificar ventana, solo velocidad para determinar si inicia combo
                Debug.Log($"[JumpCombo] Primer salto - no verificar ventana de tiempo");
            }
            
            Debug.Log($"[JumpCombo] Contador después de verificación: {currentJumpInChain}");
            
            // Determinar qué salto ejecutar basado en el contador
            switch (currentJumpInChain)
            {
                case 0:
                    jumpToExecute = normalJump;
                    Debug.Log($"[JumpCombo] ✓ Ejecutando: NORMAL JUMP (1er salto)");
                    break;
                case 1:
                    jumpToExecute = doubleJump;
                    Debug.Log($"[JumpCombo] ✓ Ejecutando: DOUBLE JUMP (2do salto)");
                    break;
                case 2:
                    jumpToExecute = tripleJump;
                    Debug.Log($"[JumpCombo] ✓ Ejecutando: TRIPLE JUMP (3er salto)");
                    break;
                case 3:
                default:
                    // Después del 3er salto, seguir con normal jump pero animación de triple
                    jumpToExecute = normalJump;
                    Debug.Log($"[JumpCombo] ✓ Ejecutando: NORMAL JUMP (post-triple, salto #{currentJumpInChain + 1})");
                    break;
            }
        }
        
        if (jumpToExecute != null)
        {
            // NUEVO: Determinar si debemos mantener la animación de triple jump
            bool shouldKeepTripleJumpAnimation = (currentJumpInChain >= 3);
            
            if (RequestJump(jumpToExecute, shouldKeepTripleJumpAnimation))
            {
                lastJumpTime = Time.time;
                jumpConsumed = true; // NUEVO: Consumir input
                
                if (jumpToExecute == normalJump || jumpToExecute == doubleJump || jumpToExecute == tripleJump)
                {
                    if (currentSpeed >= comboSpeedThreshold)
                    {
                        Debug.Log($"[JumpCombo] INCREMENTANDO contador de {currentJumpInChain} a {currentJumpInChain + 1}");
                        // CORREGIDO: Incrementar DESPUÉS de ejecutar, pero sin resetear a 0
                        // Esto permite que el contador siga creciendo después del 3er salto
                        currentJumpInChain++;
                    }
                    else
                    {
                        Debug.LogWarning($"[JumpCombo] Velocidad insuficiente para combo, reseteando contador");
                        currentJumpInChain = 0;
                    }
                }
            }
        }
    }
    
    private bool IsInputOppositeToPlayerDirection()
    {
        if (inputDirection.magnitude < 0.8f) return false;
        
        if (recentSharpTurn && Time.time < sharpTurnTime + 0.3f)
        {
            return true;
        }
        
        return false;
    }
    
    private bool RequestJump(JumpTypeCreator jumpType, bool keepTripleJumpAnimation = false)
    {
        if (jumpType == null) return false;
        
        switch (jumpType.condition)
        {
            case JumpCondition.GroundedOnly:
                if (!grounded) return false;
                break;
            case JumpCondition.AirOnly:
                if (grounded) return false;
                break;
            case JumpCondition.Both:
                break;
        }
        
        if (jumpType.rotatePlayer)
        {
            EventBus.Raise<OnRotatePlayerCommand>(new OnRotatePlayerCommand()
            {
                Player = gameObject,
                Degrees = jumpType.rotationDegrees,
                InvertMovementDirection = jumpType.invertMovementDirection
            });
        }
        
        if (jumpType.hangTime > 0f)
        {
            isInHangTime = true;
            hangTimeStart = Time.time;
            currentHangDuration = jumpType.hangTime;
            pendingJumpAfterHang = jumpType;
            
            EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
            {
                Player = gameObject,
                IsInHangTime = true
            });
            
            EventBus.Raise<OnExecuteJumpCommand>(new OnExecuteJumpCommand()
            {
                Player = gameObject,
                JumpType = jumpType,
                KeepTripleJumpAnimation = keepTripleJumpAnimation
            });
        }
        else
        {
            EventBus.Raise<OnExecuteJumpCommand>(new OnExecuteJumpCommand()
            {
                Player = gameObject,
                JumpType = jumpType,
                KeepTripleJumpAnimation = keepTripleJumpAnimation
            });
        }
        
        return true;
    }
}