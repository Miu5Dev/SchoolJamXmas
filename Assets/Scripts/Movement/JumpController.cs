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

    [Header("Config")] 
    public float delayBetweenJumps = 0.2f;
    public float jumpChainWindow = 0.4f;
    public float longjumpSpeedThreshold = 0.5f;
    
    [Header("Input Detection")]
    public float backflipInputThreshold = -0.7f;
    
    [Header("Debug")]
    [SerializeField] private int currentJumpInChain = 0;
    [SerializeField] private float lastJumpTime = -1f;
    [SerializeField] private float lastGroundedTime = -1f;
    [SerializeField] private float lastDiveTime = -1f;
    [SerializeField] private bool isDiving = false;
    [SerializeField] private float currentSpeed = 0f;
    [SerializeField] private bool grounded = false;
    [SerializeField] private bool isJumping = false;
    [SerializeField] private bool isAction = false;
    [SerializeField] private bool isCrouching = false;
    [SerializeField] private Vector2 inputDirection = Vector2.zero;
    
    void OnEnable()
    {
        EventBus.Subscribe<OnPlayerGroundedEvent>(OnGrounded);
        EventBus.Subscribe<OnPlayerAirborneEvent>(OnAirborne);
        EventBus.Subscribe<OnJumpInputEvent>(OnJumpInput);
        EventBus.Subscribe<OnMoveInputEvent>(OnMoveInput);
        EventBus.Subscribe<OnActionInputEvent>(OnActionInput);
        EventBus.Subscribe<OnCrouchInputEvent>(OnCrouchInput);
        EventBus.Subscribe<OnPlayerMoveEvent>(OnPlayerMove);
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
    }
    
    private void OnGrounded(OnPlayerGroundedEvent ev)
    {
        grounded = ev.IsGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            isDiving = false;
        }
    }
    
    private void OnAirborne(OnPlayerAirborneEvent ev)
    {
        grounded = false;
    }
    
    private void OnJumpInput(OnJumpInputEvent e)
    {
        isJumping = e.pressed;
    }
    
    private void OnActionInput(OnActionInputEvent e)
    {
        isAction = e.pressed;
    }
    
    private void OnCrouchInput(OnCrouchInputEvent e)
    {
        isCrouching = e.pressed;
    }
    
    private void OnMoveInput(OnMoveInputEvent e)
    {
        inputDirection = e.Direction;
    }
    
    private void OnPlayerMove(OnPlayerMoveEvent e)
    {
        currentSpeed = e.speed;
    }
    
    void Update()
    {
        HandleDive();
        HandleJumpLogic();
        
        // Resetear cadena si pasa mucho tiempo sin saltar después de aterrizar
        if (grounded && Time.time > lastGroundedTime + jumpChainWindow)
        {
            currentJumpInChain = 0;
        }
    }
    
    private void HandleDive()
    {
        // DIVE - Presionar action en el aire
        if (isAction && !grounded && !isDiving && dive != null && Time.time > lastDiveTime + delayBetweenJumps)
        {
            if (RequestJump(dive))
            {
                lastDiveTime = Time.time;
                isDiving = true;
                currentJumpInChain = 0;
            }
        }
    }
    
    private void HandleJumpLogic()
    {
        // BLOQUEAR saltos si está en dive
        if (isDiving) return;
        
        // Solo procesar si se presionó salto y pasó el delay
        if (!isJumping || Time.time < lastJumpTime + delayBetweenJumps)
            return;
        
        JumpTypeCreator jumpToExecute = null;
        
        // PRIORIDAD 1: LONGJUMP o BACKFLIP - Si está agachado
        if (grounded && isCrouching)
        {
            // Si tiene velocidad → Long Jump
            if (currentSpeed > longjumpSpeedThreshold && longjump != null)
            {
                jumpToExecute = longjump;
                currentJumpInChain = 0;
            }
            // Si está parado → Backflip
            else if (backflip != null)
            {
                jumpToExecute = backflip;
                currentJumpInChain = 0;
            }
        }
        // PRIORIDAD 2: BACKFLIP - Input hacia atrás
        else if (grounded && inputDirection.y < backflipInputThreshold && backflip != null)
        {
            jumpToExecute = backflip;
            currentJumpInChain = 0;
        }
        
        // PRIORIDAD 3: Combo de saltos normales (TODOS desde el suelo)
        if (jumpToExecute == null && grounded)
        {
            // Verificar si está dentro de la ventana de combo
            bool isInComboWindow = Time.time < lastGroundedTime + jumpChainWindow;
            
            if (!isInComboWindow)
            {
                currentJumpInChain = 0;
            }
            
            // Seleccionar salto según la posición en el combo
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
                
                // Incrementar combo si fue un salto normal/double/triple
                if (jumpToExecute == normalJump || jumpToExecute == doubleJump || jumpToExecute == tripleJump)
                {
                    currentJumpInChain++;
                    
                    if (currentJumpInChain > 2)
                        currentJumpInChain = 0;
                }
            }
        }
    }
    
    private bool RequestJump(JumpTypeCreator jumpType)
    {
        if (jumpType == null) return false;
        
        // Validar condiciones del salto
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
        
        // EMITIR EVENTO para que PlayerController ejecute el salto
        EventBus.Raise<OnExecuteJumpCommand>(new OnExecuteJumpCommand()
        {
            Player = gameObject,
            JumpType = jumpType
        });
        
        return true;
    }
}