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

    [Header("Config")] 
    public float delayBetweenJumps = 0.2f;
    public float jumpChainWindow = 0.4f;
    public float longjumpSpeedThreshold = 0.5f;
    public float comboSpeedThreshold = 0.3f; // Velocidad mínima para acumular combo
    
    [Header("Ground Pound Config")]
    public float groundPoundJumpWindow = 0.5f;
    public float groundPoundForce = -50f;
    
    [Header("Input Detection")]
    public float backflipInputThreshold = -0.7f;
    public float backflipDirectionChangeAngle = 120f; // Ángulo mínimo para considerar dirección opuesta
    
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
    [SerializeField] private bool isAction = false;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private bool crouchPressedInAir = false;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    
    [SerializeField] private Vector3 currentMoveDirection3D = Vector3.zero;
    [SerializeField] private Quaternion currentPlayerRotation = Quaternion.identity;
    [SerializeField] private bool recentSharpTurn = false;
    [SerializeField] private float sharpTurnTime = -1f;
    [SerializeField] private float sharpTurnAngle = 0f;
    [SerializeField] private bool backflipConsumed = false; // Flag para evitar backflip repetido
    
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
    }
    
    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = ev.IsGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            isDiving = false;
            crouchPressedInAir = false;
            backflipConsumed = false; // Resetear al tocar el suelo
            
            // Resetear ground pound al tocar el suelo
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
            
            // Cancelar hang time al tocar el suelo
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
        // Cancelar hang time si aterriza
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
        
        // Resetear backflipConsumed cuando suelta el botón de salto
        if (!e.pressed)
        {
            backflipConsumed = false;
        }
    }
    
    private void OnActionInput(OnActionInputEvent e)
    {
        isAction = e.pressed;
    }
    
    private void OnCrouchInput(OnCrouchInputEvent e)
    {
        bool wasCrouching = isCrouching;
        isCrouching = e.pressed;
        
        // Detectar si presionó crouch mientras está en el aire
        if (isCrouching && !wasCrouching && !grounded)
        {
            crouchPressedInAir = true;
        }
        
        // Resetear flag al soltar crouch
        if (!isCrouching)
        {
            crouchPressedInAir = false;
        }
    }
    
    private void OnMoveInput(OnMoveInputEvent e)
    {
        inputDirection = e.Direction;
    }
    
    private void OnPlayerMove(OnPlayerMoveEvent e)
    {
        currentSpeed = e.speed;
        
        // Convertir Direction (Vector2) a Vector3
        currentMoveDirection3D = new Vector3(e.Direction.x, 0, e.Direction.y);
        
        // Guardar rotación del jugador
        currentPlayerRotation = e.Rotation;
    }
    
    private void OnDirectionChange(OnDirectionChangeEvent ev)
    {
        // Detectar giros bruscos (cambios de dirección grandes)
        if (ev.AngleChange > backflipDirectionChangeAngle)
        {
            recentSharpTurn = true;
            sharpTurnTime = Time.time;
            sharpTurnAngle = ev.AngleChange;
        }
    }
    
    void Update()
    {
        HandleHangTime();
        HandleGroundPound();
        HandleDive();
        HandleJumpLogic();
        
        // Resetear cadena si pasa mucho tiempo sin saltar después de aterrizar
        if (grounded && Time.time > lastGroundedTime + jumpChainWindow)
        {
            currentJumpInChain = 0;
        }
        
        // Resetear ground pound jump window
        if (completedGroundPound && Time.time > groundPoundCompleteTime + groundPoundJumpWindow)
        {
            completedGroundPound = false;
        }
        
        // Resetear sharp turn window
        if (recentSharpTurn && Time.time > sharpTurnTime + 0.3f)
        {
            recentSharpTurn = false;
        }
    }
    
    private void HandleHangTime()
    {
        if (!isInHangTime) return;
        
        // Verificar si terminó el hang time
        if (Time.time >= hangTimeStart + currentHangDuration)
        {
            isInHangTime = false;
            
            EventBus.Raise<OnSetHangTimeState>(new OnSetHangTimeState()
            {
                Player = gameObject,
                IsInHangTime = false
            });
            
            // Aplicar la fuerza después del hang time
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
        // Solo permitir ground pound si presionó crouch ESTANDO en el aire
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
        
        // Actualizar si puede cancelar (solo durante hang time del ground pound)
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
        // DIVE - Presionar action en el aire
        if (isAction && !grounded && !isDiving && dive != null && Time.time > lastDiveTime + delayBetweenJumps)
        {
            // Puede cancelar ground pound durante hang time
            if (isGroundPounding && canCancelGroundPound)
            {
                if (RequestJump(dive))
                {
                    lastDiveTime = Time.time;
                    isDiving = true;
                    isGroundPounding = false;
                    canCancelGroundPound = false;
                    currentJumpInChain = 0;
                    
                    // Cancelar hang time y reactivar gravedad
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
    
    private void HandleJumpLogic()
    {
        // BLOQUEAR saltos si está en dive, ground pound o hang time
        if (isDiving || isGroundPounding || isInHangTime) return;
        
        // Solo procesar si se presionó salto y pasó el delay
        if (!isJumping || Time.time < lastJumpTime + delayBetweenJumps)
            return;
        
        JumpTypeCreator jumpToExecute = null;
        
        // PRIORIDAD 0: GROUND POUND JUMP
        if (grounded && completedGroundPound && groundPoundJump != null)
        {
            jumpToExecute = groundPoundJump;
            completedGroundPound = false;
            currentJumpInChain = 0;
        }
        
        // PRIORIDAD 1: LONGJUMP o BACKFLIP (cuando está agachado)
        else if (grounded && isCrouching)
        {
            if (currentSpeed > longjumpSpeedThreshold && longjump != null)
            {
                jumpToExecute = longjump;
                currentJumpInChain = 0;
            }
            else if (backflip != null)
            {
                jumpToExecute = backflip;
                currentJumpInChain = 0;
            }
        }
        // PRIORIDAD 2: BACKFLIP (cuando corre en dirección opuesta)
        // Solo se activa si hay un giro brusco reciente Y no se ha consumido ya
        else if (grounded && backflip != null && !backflipConsumed)
        {
            if (IsInputOppositeToPlayerDirection())
            {
                jumpToExecute = backflip;
                currentJumpInChain = 0;
                recentSharpTurn = false; // Consumir el flag del giro
                backflipConsumed = true; // Marcar como consumido para evitar repetición
            }
        }
        
        // PRIORIDAD 3: Combo de saltos normales
        if (jumpToExecute == null && grounded)
        {
            bool isInComboWindow = Time.time < lastGroundedTime + jumpChainWindow;
            bool hasEnoughSpeed = currentSpeed >= comboSpeedThreshold;
            
            if (!isInComboWindow || !hasEnoughSpeed)
            {
                currentJumpInChain = 0;
            }
            
            switch (currentJumpInChain)
            {
                case 0:
                    jumpToExecute = normalJump;
                    break;
                case 1:
                    jumpToExecute = doubleJump;
                    break;
                case 2:
                    jumpToExecute = tripleJump;
                    break;
                default:
                    jumpToExecute = normalJump;
                    currentJumpInChain = -1;
                    break;
            }
        }
        
        // Ejecutar el salto seleccionado
        if (jumpToExecute != null)
        {
            if (RequestJump(jumpToExecute))
            {
                lastJumpTime = Time.time;
                
                // Incrementar combo solo si tiene velocidad suficiente
                if (jumpToExecute == normalJump || jumpToExecute == doubleJump || jumpToExecute == tripleJump)
                {
                    if (currentSpeed >= comboSpeedThreshold)
                    {
                        currentJumpInChain++;
                        
                        if (currentJumpInChain > 2)
                            currentJumpInChain = 0;
                    }
                    else
                    {
                        currentJumpInChain = 0;
                    }
                }
            }
        }
    }
    
    private bool IsInputOppositeToPlayerDirection()
    {
        // Si no hay input suficiente, no es backflip
        if (inputDirection.magnitude < 0.8f) return false;
        
        // ÚNICO MÉTODO: Detectar giro brusco reciente (> 120°)
        // Esto asegura que el backflip solo ocurra cuando hay un cambio de dirección real
        // y no simplemente por mantener el input hacia atrás
        if (recentSharpTurn && Time.time < sharpTurnTime + 0.3f)
        {
            return true;
        }
        
        return false;
    }
    
    private bool RequestJump(JumpTypeCreator jumpType)
    {
        if (jumpType == null) return false;
        
        // Validar condiciones
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
        
        // Si el salto requiere rotación, emitir evento
        if (jumpType.rotatePlayer)
        {
            EventBus.Raise<OnRotatePlayerCommand>(new OnRotatePlayerCommand()
            {
                Player = gameObject,
                Degrees = jumpType.rotationDegrees,
                InvertMovementDirection = jumpType.invertMovementDirection
            });
        }
        
        // Si el salto tiene hang time, iniciar hang time
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
                JumpType = jumpType
            });
        }
        else
        {
            EventBus.Raise<OnExecuteJumpCommand>(new OnExecuteJumpCommand()
            {
                Player = gameObject,
                JumpType = jumpType
            });
        }
        
        return true;
    }
}